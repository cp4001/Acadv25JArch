using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;

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

        /// <summary>
        /// 선택한 Line 들에 걸쳐진 Text(덕트 폭)를 읽어 각 Line 의 "a" XData 로 기록.
        /// 대상 Text 는 Line 에 걸쳐(straddle) 있는 TEXT/MTEXT 중 Line 중심선에 가장 가까운 것.
        /// 값은 숫자 파싱 없이 TextString 을 그대로 string 으로 저장.
        /// 걸쳐진 Text 가 없는 Line 은 Yellow(2) 로 표시하고 개수 출력.
        /// </summary>
        [CommandMethod("SetDuctWidth", CommandFlags.UsePickSet)]
        public void Cmd_SetDuctWidth()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db  = doc.Database;
            Editor   ed  = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 1. Line 선택 (UsePickSet: 사전선택 표시 + 추가/제거 허용)
                    List<Line> targets = JEntity.GetEntityByTpye<Line>(
                        "덕트 Line 을 선택 하세요 (Enter 확정)?", JSelFilter.MakeFilterTypes("LINE"));
                    if (targets == null || targets.Count == 0) return;

                    tr.CheckRegName("a");

                    // 선택 Line 전체가 화면에 보이도록 Zoom fit
                    // (걸쳐진 Text 추출에 ed.SelectCrossingWindow 사용 → 뷰포트 밖이면 매핑 실패)
                    ed.ZoomToEntities(targets.Select(l => l.ObjectId));

                    int setCount     = 0;
                    int missingCount = 0;

                    foreach (var ln in targets)
                    {
                        if (ln == null) continue;

                        string width = TryGetWidthTextByLine(ln, ed, tr);

                        ln.UpgradeOpen();

                        if (!string.IsNullOrWhiteSpace(width))
                        {
                            JXdata.SetXdata(ln, "a", width);
                            setCount++;
                        }
                        else
                        {
                            ln.Color = Color.FromColorIndex(ColorMethod.ByAci, 2); // Yellow
                            missingCount++;
                        }
                    }

                    ed.WriteMessage($"\nDuct Width 설정 완료: {setCount}개");
                    ed.WriteMessage($"\n걸쳐진 Text 없는 Line: {missingCount}개 (Yellow 표시)");

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Line 에 걸쳐진(straddle) TEXT/MTEXT 중 중심선에 가장 가까운 것의 문자열을 반환.
        /// SelectCrossingWindow 로 Line bbox(축정렬 Line 대비 길이 5% 여유) 안 후보를 모은 뒤,
        /// Text 중심의 Line 수선거리가 최소이고 임계값(Text 높이) 이내인 것을 선택.
        /// </summary>
        private string TryGetWidthTextByLine(Line ln, Editor ed, Transaction tr)
        {
            Point3d s = ln.StartPoint;
            Point3d e = ln.EndPoint;

            // 축정렬 Line 은 bbox 가 납작하므로 길이 5% (최소 1.0) 만큼 여유를 줘서 후보 수집
            double margin = System.Math.Max(s.DistanceTo(e) * 0.05, 1.0);
            var min = new Point3d(System.Math.Min(s.X, e.X) - margin, System.Math.Min(s.Y, e.Y) - margin, 0);
            var max = new Point3d(System.Math.Max(s.X, e.X) + margin, System.Math.Max(s.Y, e.Y) + margin, 0);

            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "TEXT"),
                new TypedValue((int)DxfCode.Start, "MTEXT"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            });

            var psr = ed.SelectCrossingWindow(min, max, filter);
            if (psr.Status != PromptStatus.OK || psr.Value == null) return null;

            string best     = null;
            double bestDist  = double.MaxValue;

            foreach (SelectedObject so in psr.Value)
            {
                var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                string text;
                double height;
                switch (ent)
                {
                    case DBText dbt: text = dbt.TextString; height = dbt.Height;       break;
                    case MText mt:   text = mt.Text;        height = mt.ActualHeight;  break;
                    default: continue;
                }
                if (string.IsNullOrWhiteSpace(text)) continue;

                Extents3d tx;
                try { tx = ent.GeometricExtents; } catch { continue; }
                var center = new Point3d(
                    (tx.MinPoint.X + tx.MaxPoint.X) / 2.0,
                    (tx.MinPoint.Y + tx.MaxPoint.Y) / 2.0, 0);

                // Text 중심이 Line 중심선에 걸쳐 있는지: 선분 범위 내 수선거리가 Text 높이 이내
                Point3d closest = ln.GetClosestPointTo(center, false);
                double  dist    = closest.DistanceTo(center);
                double  thresh  = height > 0 ? height : margin;
                if (dist > thresh) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = text.Trim();
                }
            }
            return best;
        }
    }
}
