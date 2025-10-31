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
using CADExtension;
using RoomUtil;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Policy;
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
        //선택된 라인들을 기준점에서 각도 순서로 정렬하고, 이웃한 라인들의 교차점을 찾아 닫힌 폴리라인을 생성
        [CommandMethod(Jdf.Cmd.라인선택안목폴리만들기)]
        public void Cmd_LinesTo_ConvertClosedPolyline()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {

                    tr.CheckRegName("Arch,Room,Disp");

                    // 3개 이상의 라인 선택
                    List<Line> selectedLines = SelectLines1(ed, db);
                    selectedLines = selectedLines.Where(l => l.Length > 300.0).ToList();
                    if (selectedLines == null || selectedLines.Count < 3)
                    {
                        ed.WriteMessage("\n3개 이상의 라인을 선택해야 합니다.");
                        return;
                    }


                    // GroupLinesBySlope(selines);  

                    // 기준점 선택
                    Point3d? referencePoint = GetReferencePoint(ed);
                    if (!referencePoint.HasValue)
                    {
                        ed.WriteMessage("\n기준점이 선택되지 않았습니다.");
                        return;
                    }

                    // 같은 방위각 line 으로 Grouping
                    var pt = (Point3d)referencePoint;

                    // line 을 긴것부터 우선 처리 평행 Grouping
                    selectedLines = selectedLines.OrderByDescending(line => line.Length).ToList();
                    var lineGroups = LineGrouping.GroupLinesByAngleAndDistance(selectedLines, 1, 300);

                    List<Line> selines = new List<Line>();
                    foreach (var group in lineGroups)
                    {
                        if (group.Count == 1)
                        {
                            selines.Add(group[0]);
                            continue;
                        }
                        if (group.Count >= 2) // 그룹에 2개 이상의 라인이 있을 때만 센터 라인 생성
                        {
                            // 길이 기준 오름차순 (긴 것부터)
                            var group1 = group.OrderBy(line => line.GetCentor().DistanceTo(pt)).ToList();
                            selines.Add(group1[0]); // 기준점에 가장 가까운 라인 추가
                        }
                    }


                    // colinear line 제거 
                    selines = Func.RemoveColinearLinesKeepShortest(selines, pt);

                    selines = selines.OrderBy(ll => ll.GetAzimuth(pt)).ToList();



                    // selines 순환 페어 

                    List<Line> lll = new List<Line>();

                    var selines1 = selines.SkipLast(1).Prepend(selines.Last()).ToList();
                    var selines2 = selines.Skip(1).Append(selines.First()).ToList();
                    var gg = selines.Zip(selines2, (line1, line2) =>  // line2가  index 이후 것이다.
                    {
                        var lin1 = new Line(line2.GetClosestPointTo(pt, true), pt);  //line2.GetClosestPointTo(pt,true)
                        var lin2 = new Line(line2.GetClosestPointTo(pt, true), line1.GetCentor());
                        lin1 = line1;
                        lin2 = line2;
                        //line2.GetCentor()
                        var ang = lin1.GetAngle(lin2);
                        if((ang >70) && (ang <160))
                        {
                            lll.Add(line1);
                        }
                        return (lin1, lin2);
                    }).ToList();

                    foreach (var g in gg)
                    {
                        var ang = g.lin1.GetAngle();
                        var ang1 = g.lin2.GetAngle();
                        var def = ang - ang1;
                        var dd = g.lin1.GetAngle(g.lin2);


                        var ang2 = g.lin1.GetAzimuth(pt);
                    }




                    //// 방위각 기준으로 선을 만들떄  내측에  교차하는 선이 있으면 삭제 
                    //List<Line> selines1 = new List<Line>();
                    //foreach (var line in selines)
                    //{
                    //    var lindir = new Line(pt, line.GetCentor());
                    //    if (lindir.IsInterSect(selines)) continue;
                    //    selines1.Add(line);
                    //}


                    int ic = 0;
                    foreach (var line in lll)
                    {
                        line.AddTextAtCen(tr, pt,ic.ToString()); ic++;    
                    }

                   


                    //var groupedLines = GroupLinesByAzimuth(selines, pt);
                    //// 그룹별로 처리 기준점에서 가장 가까운 라인 선택   
                    //List<Line> glines = new List<Line>();
                    //foreach (var group in groupedLines)
                    //{
                    //    if (group.Count == 1)
                    //    {
                    //        glines.Add(group[0]);
                    //        continue;
                    //    }
                    //    // 그룹에서 가장 기준점에 가까운 라인 선택
                    //    var shortestLine = group
                    //        .OrderBy(item => item.GetCentor().DistanceTo(pt))
                    //        .First();

                    //    glines.Add(shortestLine);
                    //}


                    // 기준점을 사용하여 라인들을 각도 순서로 정렬

                    //List <Line> sortedLines = SortLinesByAngle(glines, pt);

                    List<Line> sortedLines = lll.OrderBy(ll => ll.GetAzimuth(pt)).ToList();


                    ed.WriteMessage($"\n기준점에서 각도 순서로 {sortedLines.Count}개의 라인을 정렬했습니다.");

                    // 정렬된 순서로 순환적 이웃 관계 형성
                    List<Point3d> intersectionPoints = CalculateIntersectionPoints(sortedLines);

                    //ed.WriteMessage($"\n{sortedLines.Count}개의 라인에서 {intersectionPoints.Count}개의 교차점을 찾았습니다.");

                    //if (intersectionPoints.Count != sortedLines.Count)
                    //{
                    //    ed.WriteMessage("\n예상된 교차점 수와 일치하지 않습니다.");
                    //    return;
                    //}

                    // 교차점들이 이미 올바른 순서로 생성됨 (선택 순서 = 이웃 순서)

                    // 닫힌 폴리라인 생성
                    var pl =CreateClosedPolyline(tr,intersectionPoints, db,Jdf.Layer.RoomPoly);

                    // rommPoly 지정 
                    pl.UpgradeOpen();
                    JXdata.SetXdata(pl, "Arch", "Room");
                    JXdata.SetXdata(pl, "Room", "Room");
                    JXdata.SetXdata(pl, "Disp", "room");

                    //ed.WriteMessage($"\n{intersectionPoints.Count}개의 교차점으로 닫힌 폴리라인이 생성되었습니다.");

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        [CommandMethod(Jdf.Cmd.벽센터라인선택폴리만들기)]
        public void Cmd_Wall_LinesTo_ConvertClosedPolyline()
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


                // GroupLinesBySlope(selines);  

                // 기준점 선택
                Point3d? referencePoint = GetReferencePoint(ed);
                if (!referencePoint.HasValue)
                {
                    ed.WriteMessage("\n기준점이 선택되지 않았습니다.");
                    return;
                }

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 같은 방위각 line 으로 Grouping
                    var pt = (Point3d)referencePoint;

                    // colinear line 제거 
                    var selines = Func.RemoveColinearLinesKeepShortest(selectedLines, pt);

                    // 기준점 에서 볼때 보이는 Line만 선택
                    // 4단계: 필터링 수행
                    selines = LineVisibilityFilter.FilterLinesByVisibility(pt, selines);

                    //// 방위각 기준으로 선을 만들떄  내측에  교차하는 선이 있으면 삭제 
                    List<Line> selines1 = new List<Line>();
                    //foreach (var line in selines)
                    //{
                    //    var lindir = new Line(pt, line.GetCentor());
                    //    if (line.IsInterSect(selines)) continue;
                    //    selines1.Add(line);
                    //}   

                    selines1 = selines;

                    var groupedLines = GroupLinesByAzimuth(selines1, pt);
                    // 그룹별로 처리 기준점에서 가장 가까운 라인 선택   
                    List<Line> glines = new List<Line>();
                    foreach (var group in groupedLines)
                    {
                        if (group.Count == 1)
                        {
                            glines.Add(group[0]);
                            continue;
                        }
                        // 그룹에서 가장 기준점에 가까운 라인 선택
                        var shortestLine = group
                            .OrderBy(item => item.GetCentor().DistanceTo(pt))
                            .First();

                        glines.Add(shortestLine);
                    }


                    // 기준점을 사용하여 라인들을 각도 순서로 정렬
                    List<Line> sortedLines = SortLinesByAngle(glines, pt);
                    ed.WriteMessage($"\n기준점에서 각도 순서로 {sortedLines.Count}개의 라인을 정렬했습니다.");

                    // 정렬된 순서로 순환적 이웃 관계 형성
                    List<Point3d> intersectionPoints = CalculateIntersectionPoints(sortedLines);

                    ed.WriteMessage($"\n{sortedLines.Count}개의 라인에서 {intersectionPoints.Count}개의 교차점을 찾았습니다.");

                    if (intersectionPoints.Count != sortedLines.Count)
                    {
                        ed.WriteMessage("\n예상된 교차점 수와 일치하지 않습니다.");
                        return;
                    }

                    // 교차점들이 이미 올바른 순서로 생성됨 (선택 순서 = 이웃 순서)

                    // 닫힌 폴리라인 생성
                    var pl = CreateClosedPolyline(tr,intersectionPoints, db, Jdf.Layer.RoomPoly);
                    //
                    JXdata.SetXdata(pl, "Arch", "Room");
                    JXdata.SetXdata(pl, "Room", "Room");
                    JXdata.SetXdata(pl, "Disp", "room");

                    ed.WriteMessage($"\n{intersectionPoints.Count}개의 교차점으로 닫힌 폴리라인이 생성되었습니다.");

                    tr.Commit();
                }
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
            opts.MessageForAdding = "\n라인들을 선택하세요 (3개 이상, 순서 무관): ";
            opts.AllowDuplicates = false;

            // 라인만 선택하도록 필터 설정
            TypedValue[] filterList = {
                new TypedValue((int)DxfCode.Start, "LINE"),
                new TypedValue((int)DxfCode.ExtendedDataRegAppName , "WallWidth")
            };
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
        private List<Line> SelectLines1(Editor ed, Database db)
        {
            List<Line> lines = new List<Line>();

            PromptSelectionOptions opts = new PromptSelectionOptions();
            opts.MessageForAdding = "\n라인들을 선택하세요 (3개 이상, 순서 무관): ";
            opts.AllowDuplicates = false;

            // 라인만 선택하도록 필터 설정
            TypedValue[] filterList = {
                new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE"),
                //new TypedValue((int)DxfCode.ExtendedDataRegAppName , "WallWidth")
            };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionResult selResult = ed.GetSelection(opts, filter);

            if (selResult.Status != PromptStatus.OK)
                return null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selObj in selResult.Value)
                {
                    Entity ent = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent.GetType()==  typeof(Line))
                    {
                        lines.Add(ent as Line);
                    }

                    else if (ent.GetType() == typeof(Polyline))
                    {
                        var poly = ent as Polyline;
                        var lls = poly.GetLines();
                        lines.AddRange(lls);    
                    }   
                }
                tr.Commit();
            }

            return lines;
        }

        private Point3d? GetReferencePoint(Editor ed)
        {
            PromptPointOptions pointOpts = new PromptPointOptions("\n기준점을 선택하세요: ");
            pointOpts.AllowNone = false;

            PromptPointResult pointResult = ed.GetPoint(pointOpts);

            if (pointResult.Status == PromptStatus.OK)
            {
                return pointResult.Value;
            }

            return null;
        }

        private List<Line> SortLinesByAngle(List<Line> lines, Point3d referencePoint)
        {

            if (lines == null || lines.Count < 3)
                return lines ?? new List<Line>();

            var lineWithAngles = lines.Select(line =>
            {
                Point3d center = GetLineCenter(line);
                double deltaX = center.X - referencePoint.X;
                double deltaY = center.Y - referencePoint.Y;

                // 라디안을 도(degree)로 변환하고 0~360 범위로 정규화
                double angleInRadians = Math.Atan2(deltaY, deltaX);
                double angleInDegrees = (angleInRadians * 180 / Math.PI + 360) % 360;

                return new
                {
                    Line = line,
                    Center = center,
                    Angle = angleInDegrees,
                    Distance = center.DistanceTo(referencePoint)
                };
            })
            .OrderBy(item => item.Angle)           // 1차: 각도로 정렬
            .ThenBy(item => item.Distance)         // 2차: 거리로 정렬
            .ToList();

            return lineWithAngles.Select(item => item.Line).ToList();
        }

        private Point3d GetLineCenter(Line line)
        {
            return new Point3d(
                (line.StartPoint.X + line.EndPoint.X) / 2.0,
                (line.StartPoint.Y + line.EndPoint.Y) / 2.0,
                (line.StartPoint.Z + line.EndPoint.Z) / 2.0
            );
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

        private Polyline CreateClosedPolyline(Transaction tr ,List<Point3d> points, Database db,string layerName)
        {
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                tr.CreateLayer(layerName, 45, LineWeight.LineWeight020);

                 Polyline poly = new Polyline();
                
                    // 각 점을 폴리라인에 추가
                    for (int i = 0; i < points.Count; i++)
                    {
                        poly.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);
                    }

                    // 폴리라인을 닫음
                    poly.Closed = true;

                    // 현재 레이어와 색상 사용
                    poly.Layer = layerName;

                    btr.AppendEntity(poly);
                    tr.AddNewlyCreatedDBObject(poly, true);
                    //tr.Commit();
                    return poly;    
                
            //}
        }

        // 같은 기울기 line으로 Grouping
        public List<List<Line>> GroupLinesBySlope(List<Line> lines, double tolerance = 0.01)
        {
            var groups = new List<List<Line>>();

            foreach (var line in lines)
            {
                // 각도를 0~180도 범위로 정규화 (라디안을 도로 변환)
                double angle = (line.Angle * (180.0 / Math.PI)) % 180.0;

                // 음수 각도 처리
                if (angle < 0) angle += 180.0;

                // 기존 그룹 중 허용 오차(tolerance) 내의 각도가 있는지 확인
                List<Line> matchingGroup = null;
                foreach (var group in groups)
                {
                    double groupAngle = (group[0].Angle * (180.0 / Math.PI)) % 180.0;
                    if (groupAngle < 0) groupAngle += 180.0;

                    if (Math.Abs(groupAngle - angle) < tolerance)
                    {
                        matchingGroup = group;
                        break;
                    }
                }

                // 매칭되는 그룹에 추가하거나 새 그룹 생성
                if (matchingGroup != null)
                {
                    matchingGroup.Add(line);
                }
                else
                {
                    groups.Add(new List<Line> { line });
                }
            }

            return groups;
        }

        // 같은 방위각 line으로 Grouping
        public List<List<Line>> GroupLinesByAzimuth(List<Line> lines, Point3d pt)
        {
            var groups = new List<List<Line>>();

            foreach (var line in lines)
            {
                // 각도를 0~180도 범위로 정규화 (라디안을 도로 변환)
                var azi = line.GetAzimuth(pt);                  //(line.Angle * (180.0 / Math.PI)) % 180.0;

                //// 음수 각도 처리
                //if (angle < 0) angle += 180.0;

                // 기존 그룹 중 허용 오차(tolerance) 내의 각도가 있는지 확인
                List<Line> matchingGroup = null;
                foreach (var group in groups)
                {
                    var groupAzi = (group[0].GetAzimuth(pt));// * (180.0 / Math.PI)) % 180.0;
                    //if (groupAngle < 0) groupAngle += 180.0;

                    if (Math.Abs(groupAzi - azi) < 3.0)
                    {
                        matchingGroup = group;
                        break;
                    }
                }

                // 매칭되는 그룹에 추가하거나 새 그룹 생성
                if (matchingGroup != null)
                {
                    matchingGroup.Add(line);
                }
                else
                {
                    groups.Add(new List<Line> { line });
                }
            }

            return groups;
        }
    }

}
