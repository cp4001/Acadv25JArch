using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class LineSplitter
    {
        // 허용 오차 (점이 라인 위에 있다고 판단하는 거리)
        private const double TOLERANCE = 60.0;
        
        // 분할 가능 구간 비율 (10% ~ 90%)
        private const double MIN_SPLIT_RATIO = 0.03;
        private const double MAX_SPLIT_RATIO = 0.97;

        /// <summary>
        /// 라인들 중 연결점이 다른 라인의 중간에 위치하면 해당 라인을 분할
        /// </summary>
        /// <param name="lines">원본 라인 리스트</param>
        /// <returns>분할된 라인을 포함한 새 리스트</returns>
        public static List<Line> SplitLinesAtConnections(List<Line> lines)
        {
            if (lines == null || lines.Count == 0)
                return new List<Line>();

            // 분할 정보를 저장할 딕셔너리 (라인 인덱스 → 분할점 리스트)
            var splitPointsDict = new Dictionary<int, List<Point3d>>();

            // 모든 라인 쌍을 검사
            for (int i = 0; i < lines.Count; i++)
            {
                Line line1 = lines[i];

                for (int j = 0; j < lines.Count; j++)
                {
                    if (i == j) continue; // 같은 라인은 건너뛰기

                    Line line2 = lines[j];

                    // line1의 시작점이 line2의 중간 구간에 투영되는지 확인
                    CheckAndAddSplitPoint(line1.StartPoint, line2, j, splitPointsDict);

                    // line1의 끝점이 line2의 중간 구간에 투영되는지 확인
                    CheckAndAddSplitPoint(line1.EndPoint, line2, j, splitPointsDict);
                }
            }

            // 결과 리스트 생성
            var result = new List<Line>();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // 이 라인에 대한 분할점이 있는지 확인
                if (splitPointsDict.TryGetValue(i, out var splitPoints) && splitPoints.Count > 0)
                {
                    // 분할점들을 시작점으로부터의 거리 순으로 정렬
                    var sortedPoints = SortPointsAlongLine(line, splitPoints);
                    
                    // 분할된 라인들 생성
                    var splitLines = CreateSplitLines(line, sortedPoints);
                    result.AddRange(splitLines);
                }
                else
                {
                    // 분할점이 없으면 원본 라인 추가
                    result.Add(new Line(line.StartPoint, line.EndPoint));
                }
            }

            return result;
        }

        /// <summary>
        /// 점이 라인의 분할 가능 구간에 있는지 확인하고 분할점 추가
        /// </summary>
        private static void CheckAndAddSplitPoint(Point3d point, Line targetLine, int targetLineIndex,
            Dictionary<int, List<Point3d>> splitPointsDict)
        {
            // 점을 라인에 투영
            Point3d projectedPoint = targetLine.GetClosestPointTo(point, true);

            // 투영점과 원점이 충분히 가까운지 확인
            double distanceToLine = point.DistanceTo(projectedPoint);
            if (distanceToLine > TOLERANCE)
                return; // 라인에서 너무 멀리 떨어져 있음

            // 투영점이 분할 가능 구간(10%~90%)에 있는지 확인
            if (IsPointInSplitZone(projectedPoint, targetLine))
            {
                // 분할점 추가
                if (!splitPointsDict.ContainsKey(targetLineIndex))
                {
                    splitPointsDict[targetLineIndex] = new List<Point3d>();
                }

                // 중복 점 확인 (이미 같은 위치에 분할점이 있는지)
                bool isDuplicate = splitPointsDict[targetLineIndex]
                    .Any(p => p.DistanceTo(projectedPoint) < TOLERANCE);

                if (!isDuplicate)
                {
                    splitPointsDict[targetLineIndex].Add(projectedPoint);
                }
            }
        }

        /// <summary>
        /// 투영점이 라인의 10%~90% 구간에 있는지 확인
        /// </summary>
        private static bool IsPointInSplitZone(Point3d projectedPoint, Line line)
        {
            double totalLength = line.Length;
            
            if (totalLength < TOLERANCE)
                return false; // 라인이 너무 짧음

            // 시작점으로부터의 거리
            double distanceFromStart = line.StartPoint.DistanceTo(projectedPoint);
            
            // 끝점으로부터의 거리
            double distanceFromEnd = line.EndPoint.DistanceTo(projectedPoint);

            // 시작점과 끝점 중 하나와 너무 가까우면 제외
            double minDistance = totalLength * MIN_SPLIT_RATIO;
            double maxDistance = totalLength * MAX_SPLIT_RATIO;

            // 투영점이 10% 이상, 90% 이하 구간에 있는지 확인
            return distanceFromStart >= minDistance && distanceFromStart <= maxDistance;
        }

        /// <summary>
        /// 분할점들을 라인의 시작점으로부터 거리 순으로 정렬
        /// </summary>
        private static List<Point3d> SortPointsAlongLine(Line line, List<Point3d> points)
        {
            return points
                .OrderBy(p => line.StartPoint.DistanceTo(p))
                .ToList();
        }

        /// <summary>
        /// 라인을 분할점들 기준으로 여러 개의 라인으로 분할
        /// </summary>
        private static List<Line> CreateSplitLines(Line originalLine, List<Point3d> sortedSplitPoints)
        {
            var result = new List<Line>();

            // 시작점 + 분할점들 + 끝점으로 전체 점 리스트 구성
            var allPoints = new List<Point3d> { originalLine.StartPoint };
            allPoints.AddRange(sortedSplitPoints);
            allPoints.Add(originalLine.EndPoint);

            // 연속된 점들 사이에 라인 생성
            for (int i = 0; i < allPoints.Count - 1; i++)
            {
                Point3d start = allPoints[i];
                Point3d end = allPoints[i + 1];

                // 두 점이 너무 가까우면 건너뛰기
                if (start.DistanceTo(end) > TOLERANCE)
                {
                    result.Add(new Line(start, end));
                }
            }

            return result;
        }

        /// <summary>
        /// 테스트용 커맨드: 선택한 라인들을 분할
        /// </summary>
        [CommandMethod("c_SPLITLINES")]
        public void SplitLinesCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 라인들 선택
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 라인이 없습니다.");
                    return;
                }

                // 2단계: Line 객체 로드
                using var tr = db.TransactionManager.StartTransaction();

                var lines = selectedLineIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(line => line != null)
                    .ToList();

                if (lines.Count == 0)
                {
                    ed.WriteMessage("\n유효한 라인을 로드할 수 없습니다.");
                    tr.Commit();
                    return;
                }

                tr.Commit();

                // 3단계: 라인 분할 수행
                var splitLines = SplitLinesAtConnections(lines);

                // 4단계: 원본 라인 삭제 및 새 라인 추가
                using var tr2 = db.TransactionManager.StartTransaction();

                // 원본 라인 삭제
                foreach (var lineId in selectedLineIds)
                {
                    var lineToDelete = tr2.GetObject(lineId, OpenMode.ForWrite) as Line;
                    if (lineToDelete != null)
                    {
                        lineToDelete.Erase();
                    }
                }

                // 새 라인 추가
                BlockTable bt = tr2.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr2.GetObject(bt[BlockTableRecord.ModelSpace], 
                    OpenMode.ForWrite) as BlockTableRecord;

                int addedCount = 0;
                foreach (var newLine in splitLines)
                {
                    btr.AppendEntity(newLine);
                    tr2.AddNewlyCreatedDBObject(newLine, true);
                    addedCount++;
                }

                tr2.Commit();

                // 5단계: 결과 출력
                ed.WriteMessage($"\n원본 라인: {selectedLineIds.Count}개");
                ed.WriteMessage($"\n분할 후 라인: {addedCount}개");
                ed.WriteMessage($"\n분할 완료!");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 라인 선택 메서드
        /// </summary>
        private List<ObjectId> SelectLines(Editor ed)
        {
            var lineIds = new List<ObjectId>();

            // 라인만 선택하도록 필터 설정
            TypedValue[] filterList = [
                new TypedValue((int)DxfCode.Start, "LINE")
            ];
            var filter = new SelectionFilter(filterList);

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = "\n분할할 라인들을 선택하세요: "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                lineIds.AddRange(psr.Value.GetObjectIds());
            }

            return lineIds;
        }
    }

    public class LineIntersectionFinder
    {
        private const double TOLERANCE = 1e-6; // 중복점 판단 허용 오차
        private const double MAX_SEARCH_DISTANCE = 300.0; // 최대 탐색 거리
        private const double SNAP_TOLERANCE = 1.0; // 점 감지 공차

        [CommandMethod("FindIntersections")]
        public void FindIntersections()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: Line 선택
                var selectedLines = SelectLines(ed, db);
                if (selectedLines.Count < 2)
                {
                    ed.WriteMessage("\n최소 2개 이상의 Line을 선택해야 합니다.");
                    return;
                }

                ed.WriteMessage($"\n{selectedLines.Count}개의 Line이 선택되었습니다.");

                // Step 2-3: 각 Line별 교차점 찾기 (방향 기반)
                var allIntersectionPoints = new List<Point3d>();

                foreach (var line in selectedLines)
                {
                    var intersectionPoints = GetIntersectPoint(line, selectedLines);
                    allIntersectionPoints.AddRange(intersectionPoints); 
                }

                // Step 4: 중복점 제거
                var uniquePoints = RemoveDuplicatePoints(allIntersectionPoints);

                // 결과 출력
                ed.WriteMessage($"\n\n=== 결과 ===");
                ed.WriteMessage($"\n총 교차점 수 (중복 포함): {allIntersectionPoints.Count}");
                ed.WriteMessage($"\n고유 교차점 수 (중복 제거): {uniquePoints.Count}");
                ed.WriteMessage($"\n\n교차점 좌표:");

                for (int i = 0; i < uniquePoints.Count; i++)
                {
                    var pt = uniquePoints[i];
                    ed.WriteMessage($"\n  점 {i + 1}: ({pt.X:F3}, {pt.Y:F3}, {pt.Z:F3})");
                }

                // 선택 사항: 교차점에 Circle 표시
                DrawIntersectionMarkers(uniquePoints, db);
                ed.WriteMessage($"\n\n교차점 위치에 원(Circle)이 표시되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Line들을 교차점까지 연장 (방향 기반 개선 버전)
        /// </summary>
        [CommandMethod("ExtendToIntersections")]
        public void ExtendToIntersections()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: Line 선택 (ObjectId로 저장)
                var selectedLineIds = SelectLinesAsObjectIds(ed);
                if (selectedLineIds.Count < 2)
                {
                    ed.WriteMessage("\n최소 2개 이상의 Line을 선택해야 합니다.");
                    return;
                }

                ed.WriteMessage($"\n{selectedLineIds.Count}개의 Line이 선택되었습니다.");

                int extendedCount = 0;

                // Transaction으로 Line 수정
                using var tr = db.TransactionManager.StartTransaction();

                // 모든 Line 객체를 먼저 로드 (읽기 전용)
                var selectedLines = new List<Line>();
                foreach (var id in selectedLineIds)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Line line)
                    {
                        selectedLines.Add(line);
                    }
                }

                // 각 Line을 순회하며 연장 처리
                foreach (var line in selectedLines)
                {
                    line.UpgradeOpen(); // 쓰기 모드로 전환
                    var intersectionPoints = GetIntersectPoint(line, selectedLines);

                    foreach(var pt in intersectionPoints)
                    {
                        if(line.StartPoint.DistanceTo(pt) < line.EndPoint.DistanceTo(pt))
                        {
                            line.StartPoint = pt;
                        }
                        else
                        {
                            line.EndPoint = pt;
                        }

                    }
                }

                tr.Commit();

                ed.WriteMessage($"\n\n=== 연장 완료 ===");
                ed.WriteMessage($"\n총 {extendedCount}개의 끝점이 연장되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// Line 선택 메서드 (Line 객체 반환)
        /// </summary>
        private List<Line> SelectLines(Editor ed, Database db)
        {
            var lines = new List<Line>();

            TypedValue[] filterList = [
                new TypedValue((int)DxfCode.Start, "LINE")
            ];
            var filter = new SelectionFilter(filterList);

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = "\n교차점을 찾을 Line들을 선택하세요: "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                using var tr = db.TransactionManager.StartTransaction();

                foreach (ObjectId id in psr.Value.GetObjectIds())
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Line line)
                    {
                        lines.Add(line);
                    }
                }

                tr.Commit();
            }

            return lines;
        }

        /// <summary>
        /// Line 선택 메서드 (ObjectId 반환)
        /// </summary>
        private List<ObjectId> SelectLinesAsObjectIds(Editor ed)
        {
            var lineIds = new List<ObjectId>();

            TypedValue[] filterList = [
                new TypedValue((int)DxfCode.Start, "LINE")
            ];
            var filter = new SelectionFilter(filterList);

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = "\n연장할 Line들을 선택하세요: "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                lineIds.AddRange(psr.Value.GetObjectIds());
            }

            return lineIds;
        }


        /// <summary>
        /// 중복점 제거
        /// </summary>
        private List<Point3d> RemoveDuplicatePoints(List<Point3d> points)
        {
            var uniquePoints = new List<Point3d>();

            foreach (var point in points)
            {
                bool isDuplicate = false;

                foreach (var uniquePoint in uniquePoints)
                {
                    double distance = point.DistanceTo(uniquePoint);
                    if (distance < TOLERANCE)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    uniquePoints.Add(point);
                }
            }

            return uniquePoints;
        }

        /// <summary>
        /// 교차점 위치에 Circle 마커 표시
        /// </summary>
        private void DrawIntersectionMarkers(List<Point3d> points, Database db)
        {
            using var tr = db.TransactionManager.StartTransaction();

            var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            var btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            foreach (var point in points)
            {
                var circle = new Circle
                {
                    Center = point,
                    Radius = 15.0,
                    ColorIndex = 1
                };
                circle.LineWeight= LineWeight.LineWeight050;

                btr.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
            }

            tr.Commit();
        }

        /// <summary>
        /// 대상 라인과 나머지 라인들의 교차점(확장선 포함)을 찾아서
        /// 대상 라인의 중심점에서 가장 가까운 2개를 반환
        /// </summary>
        /// <param name="line">대상 라인</param>
        /// <param name="lines">비교할 라인들의 리스트 (대상 라인 포함)</param>
        /// <returns>중심점에서 가장 가까운 교차점 리스트 (최대 2개)</returns>
        private List<Point3d> GetIntersectPoint(Line line, List<Line> lines)
        {
            try
            {
                // 1단계: 라인의 중심점 계산
                Point3d centerPoint = new Point3d(
                    (line.StartPoint.X + line.EndPoint.X) / 2.0,
                    (line.StartPoint.Y + line.EndPoint.Y) / 2.0,
                    0.0
                );

                // 2단계: 모든 교차점 수집
                var intersectPoints = new List<Point3d>();

                // 선언: (string Name, int Age) 형태의 튜플을 담는 리스트
                var intersectPtLs = new List<(Point3d Pt, Double Dist)>(); // 교차점 대상과  교차점 사이의  거리 

                foreach (var otherLine in lines)
                {
                    // 자기 자신은 제외
                    if (otherLine.Handle == line.Handle)
                        continue;

                    // IntersectWith로 교차점 찾기 (ExtendBoth: 양쪽 확장)
                    using (Point3dCollection points = new Point3dCollection())
                    {
                        line.IntersectWith(
                            otherLine,
                            Intersect.ExtendBoth,  // 양쪽 라인을 확장하여 교차점 찾기
                            points,
                            IntPtr.Zero,
                            IntPtr.Zero
                        );

                        // 찾은 교차점들을 리스트에 추가
                        foreach (Point3d pt in points)
                        {
                            intersectPoints.Add(pt);
                            var dist = otherLine.GetClosestPointTo(pt,false).DistanceTo(pt);
                            var dist1 = line.GetClosestPointTo(pt, false).DistanceTo(pt);
                            intersectPtLs.Add((pt, dist>dist1 ? dist:dist1)); 
                        }
                    }
                }

                // 교차점이 없으면 빈 리스트 반환
                
                  if (intersectPoints.Count == 0)
                    return new List<Point3d>();

                // 2.5단계: line 위에 있는 점 제외 (단, StartPoint와 EndPoint는 유지)
                const double tolerance = 1e-6;
                var filteredPoints = new List<Point3d>();
                var filteredPointsDists = new List<(Point3d Pt,Double Dist)>();
                foreach (var inpt in intersectPtLs)
                {
                    //// StartPoint 또는 EndPoint인지 확인
                    //Point3d pt = inpt.Pt;
                    //bool isStartPoint = pt.DistanceTo(line.StartPoint) < tolerance;
                    //bool isEndPoint = pt.DistanceTo(line.EndPoint) < tolerance;

                    //// StartPoint나 EndPoint면 무조건 포함
                    //if (isStartPoint || isEndPoint)
                    //{
                    //    filteredPoints.Add(pt);
                    //    filteredPointsDists.Add((inpt.Pt, inpt.Dist));
                    //    continue;
                    //}

                    //// line 위의 가장 가까운 점 찾기
                    //Point3d closestPoint = line.GetClosestPointTo(pt, false);
                    //double distanceToLine = pt.DistanceTo(closestPoint);

                    //// line 위에 있으면 제외 (확장선 위의 점만 포함)
                    //if (distanceToLine > tolerance)
                    //{
                    //    filteredPoints.Add(pt);
                    //    filteredPointsDists.Add((inpt.Pt, inpt.Dist));    
                    //}
                    // line 위에 있지만 StartPoint도 EndPoint도 아니면 제외됨
                }

                //// 필터링 후 교차점이 없으면 빈 리스트 반환
                //if (filteredPoints.Count == 0)
                //    return new List<Point3d>();

                // 3단계: 중심점에서 가까운 순으로 정렬하고 상위 2개 선택
                var closestPoints = filteredPoints
                    .OrderBy(pt => pt.DistanceTo(centerPoint))
                    .Take(Math.Min(2, filteredPoints.Count))  // 최대 2개, 1개일 때는 1개만
                    .ToList();

                // 3.1단계: 중심점에서 가까운 순으로 정렬하고 상위 2개 선택
                var closestPoints1 = intersectPtLs
                    .OrderBy(ptd => ptd.Dist)
                    .Where(ptd => ptd.Dist <= MAX_SEARCH_DISTANCE) // 최대 탐색 거리 이내
                    .Take(Math.Min(2, intersectPtLs.Count))  // 최대 2개, 1개일 때는 1개만
                    .Select(xx => xx.Pt)
                    .ToList();

                //return closestPoints;
                return closestPoints1;
            }
            catch (System.Exception)
            {
                // 에러 발생 시 빈 리스트 반환
                return new List<Point3d>();
            }
        }
    }
}
