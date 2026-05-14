using AcadFunction;
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
    public class DuctTreeCommand
    {
        /// <summary>
        /// 덕트 Tree 분석 — Line + Block(디퓨져, CMH XData 보유) 선택, Root Line 지정,
        /// Leaf Block CMH 를 부하로 인식 → 각 Line 누적값을 Total_CMH XData 로 기록.
        /// (덕트 Spec 산정은 추후 추가)
        /// </summary>
        [CommandMethod("DUCTTREE")]
        public void Cmd_DuctTree()
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
                { MessageForAdding = "\nLine + 디퓨져 Block 들을 선택하세요: " };
                var psr = ed.GetSelection(selOpts, selFilter);
                if (psr.Status != PromptStatus.OK) return;

                // 2. Root Line 선택
                var peo = new PromptEntityOptions("\nRoot Line 을 선택하세요: ");
                peo.SetRejectMessage("\nLine 만 선택 가능합니다.");
                peo.AddAllowedClass(typeof(Line), true);
                var per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK) return;

                // 3. Pre-Transaction — Line 끝점 메타 + CMH 있는 Block handle 만 수집
                string rootLayer;
                string rootHandle;
                var lineEndpoints   = new List<(string handle, Point3d s, Point3d e)>();
                var cmhBlockHandles = new HashSet<string>();
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
                        else if (ent is BlockReference br && DuctTreeBuilder.HasCmh(br))
                            cmhBlockHandles.Add(br.Handle.ToString());
                    }
                    trPre.Commit();
                }

                ed.WriteMessage($"\n[Pre] rootLayer={rootLayer}, lines={lineEndpoints.Count}, CMH blocks={cmhBlockHandles.Count}");

                // 3.5. Zoom fit — 선택 Entity 전체가 화면에 보이도록
                // (SelectCrossingWindow 가 뷰포트 범위 기준이므로 분석 전 필수)
                ed.ZoomToEntities(psr.Value.GetObjectIds());

                // 4. Tree 분석 → Leaf tp 에서 CrossingWindow 로 CMH Block 매핑
                var blockToLine = DuctTreeBuilder.MapLeafTerminalsToCmhBlocks(
                    ed, rootHandle, lineEndpoints, cmhBlockHandles, margin: 10.0);

                // 5. Main Transaction — Entity 재취득 (CMH 없는 Block 은 제외)
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var rootLine = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
                    if (rootLine == null) { tr.Commit(); return; }

                    var lines = psr.Value.GetObjectIds()
                        .Select(id => tr.GetObject(id, OpenMode.ForRead) as Entity)
                        .OfType<Line>()
                        .Where(l => l.Layer == rootLine.Layer)
                        .ToList();

                    var blocks = psr.Value.GetObjectIds()
                        .Select(id => tr.GetObject(id, OpenMode.ForRead) as Entity)
                        .OfType<BlockReference>()
                        .Where(br => cmhBlockHandles.Contains(br.Handle.ToString()))
                        .ToList();

                    ed.WriteMessage($"\n레이어 [{rootLine.Layer}] 필터: Line {lines.Count}개, CMH Block {blocks.Count}개, 매핑 {blockToLine.Count}건");

                    // 6. Tree 구성 + 누적 부하 집계
                    var builder  = new DuctTreeBuilder();
                    var rootNode = builder.BuildTree(rootLine, lines, blocks, blockToLine);
                    builder.CalculateLoads(rootNode);

                    int totalDiffuser = builder.CountBlockNodes(rootNode);
                    int totalLines    = builder.CountLineNodes(rootNode);
                    ed.WriteMessage($"\n[DuctTree] Line: {totalLines}개, Diffuser: {totalDiffuser}개, Root 총 풍량: {rootNode.Load:F1} CMH");

                    tr.Commit();

                    // 7. Form 표시 — XData 저장은 Form 의 Apply 버튼에서 (FCU 패턴)
                    Application.ShowModelessDialog(
                        new DuctTreeForm(rootNode, rootLine.Layer, db));
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }
    }
}
