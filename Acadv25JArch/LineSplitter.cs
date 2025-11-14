using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
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

                // Step 2-3: 각 Line별 교차점 찾기
                var allIntersectionPoints = new List<Point3d>();

                foreach (var line in selectedLines)
                {
                    // StartPoint에서 가장 가까운 Line 찾기
                    var closestLineFromStart = FindClosestLineFromPoint(line.StartPoint, line, selectedLines);
                    if (closestLineFromStart != null)
                    {
                        var intersectPoints = FindIntersectionPoints(line, closestLineFromStart);
                        allIntersectionPoints.AddRange(intersectPoints);
                    }

                    // EndPoint에서 가장 가까운 Line 찾기
                    var closestLineFromEnd = FindClosestLineFromPoint(line.EndPoint, line, selectedLines);
                    if (closestLineFromEnd != null)
                    {
                        var intersectPoints = FindIntersectionPoints(line, closestLineFromEnd);
                        allIntersectionPoints.AddRange(intersectPoints);
                    }
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
        /// Line 선택 메서드
        /// </summary>
        private List<Line> SelectLines(Editor ed, Database db)
        {
            var lines = new List<Line>();

            // Line만 선택하도록 필터 설정
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
                        // Line 객체를 복사하여 리스트에 추가 (Transaction 외부에서 사용하기 위해)
                        lines.Add(line);
                    }
                }

                tr.Commit();
            }

            return lines;
        }

        /// <summary>
        /// 특정 점에서 가장 가까운 Line 찾기 (자기 자신 제외)
        /// </summary>
        private Line FindClosestLineFromPoint(Point3d point, Line currentLine, List<Line> allLines)
        {
            Line closestLine = null;
            double minDistance = double.MaxValue;

            foreach (var line in allLines)
            {
                // 자기 자신은 제외
                if (line.Handle == currentLine.Handle)
                    continue;

                // GetClosestPointTo를 사용하여 점에서 Line까지의 최단 거리 계산
                Point3d closestPoint = line.GetClosestPointTo(point, false); // false: 연장선 포함
                double distance = point.DistanceTo(closestPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLine = line;
                }
            }

            return closestLine;
        }

        /// <summary>
        /// 두 Line의 교차점 찾기 (연장선 포함)
        /// </summary>
        private List<Point3d> FindIntersectionPoints(Line line1, Line line2)
        {
            var points = new List<Point3d>();

            try
            {
                var intersectPoints = new Point3dCollection();

                // IntersectWith 메서드 사용 (Intersect.ExtendBoth: 양쪽 연장선 포함)
                line1.IntersectWith(
                    line2,
                    Intersect.ExtendBoth,  // 양쪽 Line 모두 연장하여 교차점 찾기
                    new Plane(),           // XY 평면
                    intersectPoints,
                    IntPtr.Zero,
                    IntPtr.Zero
                );

                // Point3dCollection을 List<Point3d>로 변환
                foreach (Point3d pt in intersectPoints)
                {
                    points.Add(pt);
                }
            }
            catch (System.Exception)
            {
                // 교차점이 없거나 오류 발생 시 빈 리스트 반환
            }

            return points;
        }

        /// <summary>
        /// 중복점 제거 (허용 오차 범위 내의 점들을 하나로 통합)
        /// </summary>
        private List<Point3d> RemoveDuplicatePoints(List<Point3d> points)
        {
            var uniquePoints = new List<Point3d>();

            foreach (var point in points)
            {
                bool isDuplicate = false;

                // 기존 고유 점들과 비교
                foreach (var uniquePoint in uniquePoints)
                {
                    double distance = point.DistanceTo(uniquePoint);
                    if (distance < TOLERANCE)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                // 중복이 아니면 추가
                if (!isDuplicate)
                {
                    uniquePoints.Add(point);
                }
            }

            return uniquePoints;
        }

        /// <summary>
        /// 교차점 위치에 Circle 마커 표시 (선택 사항)
        /// </summary>
        private void DrawIntersectionMarkers(List<Point3d> points, Database db)
        {
            using var tr = db.TransactionManager.StartTransaction();

            var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            var btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            foreach (var point in points)
            {
                // 작은 원 생성 (반지름 5)
                var circle = new Circle
                {
                    Center = point,
                    Radius = 5.0,
                    ColorIndex = 1 // 빨간색
                };

                btr.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
            }

            tr.Commit();
        }
    }
}
