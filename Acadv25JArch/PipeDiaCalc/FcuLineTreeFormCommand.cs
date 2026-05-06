using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    public class FcuLineTreeFormCommand
    {
        /// <summary>
        /// FCU 배관 Tree 분석 — Line + Block(FCU) 선택, Root Line 지정, H-W 공식으로 관경 결정.
        /// </summary>
        [CommandMethod("FCULINETREE")]
        public void Cmd_FcuLineTreeForm()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            try
            {
                // 1. Line + Block 통합 선택 (필터: LINE, INSERT)
                TypedValue[] filter =
                {
                    new TypedValue((int)DxfCode.Operator, "<OR"),
                    new TypedValue((int)DxfCode.Start,    "LINE"),
                    new TypedValue((int)DxfCode.Start,    "INSERT"),
                    new TypedValue((int)DxfCode.Operator, "OR>")
                };
                var selFilter = new SelectionFilter(filter);
                var selOpts   = new PromptSelectionOptions
                { MessageForAdding = "\nLine + FCU Block 들을 선택하세요: " };
                var psr = ed.GetSelection(selOpts, selFilter);
                if (psr.Status != PromptStatus.OK) return;

                // 2. Root Line 선택
                var peo = new PromptEntityOptions("\nRoot Line을 선택하세요: ");
                peo.SetRejectMessage("\nLine만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Line), true);
                var per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                // 3. 모드 선택 (Supply=공급 H-W, Drain=배수 수량)
                var mOpts = new PromptKeywordOptions("\n계산 모드? [공급(Supply)/배수(Drain)]");
                mOpts.Keywords.Add("Supply");
                mOpts.Keywords.Add("Drain");
                mOpts.Keywords.Default = "Supply";
                mOpts.AllowNone        = true;
                var mRes = ed.GetKeywords(mOpts);
                var mode = FcuLineTreeBuilder.CalcMode.Supply;
                if (mRes.Status == PromptStatus.OK && mRes.StringResult == "Drain")
                    mode = FcuLineTreeBuilder.CalcMode.Drain;

                // 4. 수두 입력 (Supply 모드만)
                int head = 20;
                if (mode == FcuLineTreeBuilder.CalcMode.Supply)
                {
                    var hOpts = new PromptKeywordOptions("\n수두(mmAq/m)? ");
                    hOpts.Keywords.Add("10");
                    hOpts.Keywords.Add("16");
                    hOpts.Keywords.Add("20");
                    hOpts.Keywords.Add("30");
                    hOpts.Keywords.Default = "20";
                    hOpts.AllowNone        = true;
                    var hRes = ed.GetKeywords(hOpts);
                    if (hRes.Status == PromptStatus.OK && int.TryParse(hRes.StringResult, out int h)) head = h;
                }

                // 5. Pre-Transaction — Line 끝점 메타 + LPM 있는 Block handle만 수집
                string rootLayer;
                string rootHandle;
                var lineEndpoints   = new List<(string handle, Point3d s, Point3d e)>();
                var lpmBlockHandles = new HashSet<string>();
                using (var trPre = db.TransactionManager.StartTransaction())
                {
                    var rootLnPre = trPre.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
                    if (rootLnPre == null) { trPre.Commit(); return; }
                    rootLayer  = rootLnPre.Layer;
                    rootHandle = rootLnPre.Handle.ToString();

                    foreach (var id in psr.Value.GetObjectIds())
                    {
                        var ent = trPre.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent is Line ln && ln.Layer == rootLayer)
                            lineEndpoints.Add((ln.Handle.ToString(), ln.StartPoint, ln.EndPoint));
                        else if (ent is BlockReference br && FcuLineTreeBuilder.HasLpm(br))
                            lpmBlockHandles.Add(br.Handle.ToString());
                    }
                    trPre.Commit();
                }

                ed.WriteMessage($"\n[Pre] rootLayer={rootLayer}, lines={lineEndpoints.Count}, LPM blocks={lpmBlockHandles.Count}");

                // 5.5. Zoom fit — 선택된 Entity 전체가 화면에 보이도록
                // (CrossingWindow는 화면 표시 영역 기준이므로 분석 전 필수)
                ZoomToEntities(ed, db, psr.Value.GetObjectIds());

                // 6. Tree 분석 → Leaf tp(미연결 끝점)에서 CrossingWindow로 LPM Block 매핑
                var blockToLine = FcuLineTreeBuilder.MapLeafTerminalsToLpmBlocks(
                    ed, rootHandle, lineEndpoints, lpmBlockHandles, margin: 10.0);

                // 7. Main Transaction — Entity 재취득 (LPM 없는 Block은 제외)
                using var tr = db.TransactionManager.StartTransaction();

                var rootLine = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
                if (rootLine == null) { tr.Commit(); return; }

                var lines  = psr.Value.GetObjectIds()
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Entity)
                    .OfType<Line>()
                    .Where(l => l.Layer == rootLine.Layer)
                    .ToList();

                var blocks = psr.Value.GetObjectIds()
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Entity)
                    .OfType<BlockReference>()
                    .Where(br => lpmBlockHandles.Contains(br.Handle.ToString()))
                    .ToList();

                ed.WriteMessage($"\n레이어 [{rootLine.Layer}] 필터: Line {lines.Count}개, LPM Block {blocks.Count}개, 매핑 {blockToLine.Count}건");

                // 8. Tree 구성 + 부하 집계 + 관경 계산
                var builder  = new FcuLineTreeBuilder();
                var rootNode = builder.BuildTree(rootLine, lines, blocks, blockToLine);
                builder.CalculateLoads(rootNode);
                builder.CalculateDiameters(rootNode, head, mode);

                int totalFcu = builder.CountBlockNodes(rootNode);
                string modeTag = mode == FcuLineTreeBuilder.CalcMode.Drain ? "Drain" : "Supply";
                ed.WriteMessage($"\n[{modeTag}] FCU: {totalFcu}개, 총 유량: {rootNode.Load:F1} LPM, Root 관경: {rootNode.Diameter}");

                tr.Commit();

                // 9. Form 표시
                Application.ShowModelessDialog(
                    new FcuLineTreeForm(rootNode, rootLine.Layer, head, db, mode));
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 지정된 Entity 집합의 GeometricExtents를 합산해 현재 뷰를 Zoom fit 한다.
        /// </summary>
        private static void ZoomToEntities(Editor ed, Database db, ObjectId[] selIds)
        {
            var ext = new Extents3d();
            bool initialized = false;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var id in selIds)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;
                    try
                    {
                        if (!initialized) { ext = ent.GeometricExtents; initialized = true; }
                        else ext.AddExtents(ent.GeometricExtents);
                    }
                    catch { }
                }
                tr.Commit();
            }

            if (!initialized) return;

            double w = ext.MaxPoint.X - ext.MinPoint.X;
            double h = ext.MaxPoint.Y - ext.MinPoint.Y;
            if (w <= 0) w = 1;
            if (h <= 0) h = 1;

            using (var view = ed.GetCurrentView())
            {
                view.Width       = w * 1.1;
                view.Height      = h * 1.1;
                view.CenterPoint = new Point2d(
                    (ext.MinPoint.X + ext.MaxPoint.X) * 0.5,
                    (ext.MinPoint.Y + ext.MaxPoint.Y) * 0.5);
                ed.SetCurrentView(view);
            }
            ed.UpdateScreen();
        }
    }
}
