using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace Acadv25JArch
{
    public class LineToPolylineConverter
    {
        [CommandMethod("LINES2POLY")]
        public void ConvertLinesToClosedPolyline()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // 3개 이상의 라인 선택
                List<Line> selectedLines = SelectLines(ed, db);
                if (selectedLines == null || selectedLines.Count < 3)
                {
                    ed.WriteMessage("\n3개 이상의 라인을 선택해야 합니다.");
                    return;
                }

                // 이웃하지 않는 라인들은 평행해도 괜찮음

                // 이웃한 라인들의 교차점 계산 (순환적으로)
                List<Point3d> intersectionPoints = CalculateIntersectionPoints(selectedLines);

                ed.WriteMessage($"\n{selectedLines.Count}개의 라인에서 {intersectionPoints.Count}개의 교차점을 찾았습니다.");

                if (intersectionPoints.Count != selectedLines.Count)
                {
                    ed.WriteMessage("\n예상된 교차점 수와 일치하지 않습니다.");
                    return;
                }

                // 교차점들이 이미 올바른 순서로 생성됨 (선택 순서 = 이웃 순서)

                // 닫힌 폴리라인 생성
                CreateClosedPolyline(intersectionPoints, db);

                ed.WriteMessage($"\n{intersectionPoints.Count}개의 교차점으로 닫힌 폴리라인이 생성되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        private List<Line> SelectLines(Editor ed, Database db)
        {
            List<Line> lines = new List<Line>();

            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "\n라인들을 이웃 순서대로 선택하세요 (3개 이상): ";
            opts.AllowDuplicates = false;

            // 라인만 선택하도록 필터 설정
            TypedValue[] filterList = { new TypedValue((int)DxfCode.Start, "LINE") };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionResult selResult = ed.GetSelection(opts, filter);

            if (selResult.Status != PromptStatus.OK)
                return null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selObj in selResult.Value)
                {
                    Line line = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Line;
                    if (line != null)
                    {
                        lines.Add(line);
                    }
                }
                tr.Commit();
            }

            return lines;
        }

        private List<Point3d> CalculateIntersectionPoints(List<Line> lines)
        {
            List<Point3d> intersections = new List<Point3d>();
            int lineCount = lines.Count;

            // 순환적으로 이웃한 라인들의 교차점 찾기
            for (int i = 0; i < lineCount; i++)
            {
                int nextIndex = (i + 1) % lineCount; // 순환: 마지막 라인은 첫 번째 라인과 이웃

                Point3d? intersection = FindLineIntersection(lines[i], lines[nextIndex]);
                if (intersection.HasValue)
                {
                    intersections.Add(intersection.Value);
                }
                else
                {
                    // 이웃한 라인이 평행한 경우
                    throw new System.Exception($"라인 {i}와 라인 {nextIndex}가 평행합니다. 이웃한 라인들은 평행하면 안됩니다.");
                }
            }

            return intersections;
        }

        private Point3d? FindLineIntersection(Line line1, Line line2)
        {
            // 두 직선의 교차점을 찾는 함수 (직선을 무한히 연장한 교차점)
            // 평행한 직선들은 교차점이 없으므로 null 반환
            Vector3d dir1 = line1.EndPoint - line1.StartPoint;
            Vector3d dir2 = line2.EndPoint - line2.StartPoint;
            Vector3d diff = line2.StartPoint - line1.StartPoint;

            double cross = dir1.X * dir2.Y - dir1.Y * dir2.X;

            // 평행한 경우
            if (Math.Abs(cross) < 1e-10)
                return null;

            double t1 = (diff.X * dir2.Y - diff.Y * dir2.X) / cross;

            Point3d intersection = line1.StartPoint + t1 * dir1;
            return intersection;
        }

        private void CreateClosedPolyline(List<Point3d> points, Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                using (Polyline poly = new Polyline())
                {
                    // 각 점을 폴리라인에 추가
                    for (int i = 0; i < points.Count; i++)
                    {
                        poly.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);
                    }

                    // 폴리라인을 닫음
                    poly.Closed = true;

                    // 현재 레이어와 색상 사용
                    poly.SetDatabaseDefaults();

                    btr.AppendEntity(poly);
                    tr.AddNewlyCreatedDBObject(poly, true);
                }

                tr.Commit();
            }
        }
    }

}
