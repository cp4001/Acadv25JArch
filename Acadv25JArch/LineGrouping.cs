using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;

namespace Acadv25JArch
{
    public class LineGrouping
    {
        // .NET 8.0 기능: 컴파일 타임 상수
        private const double DEFAULT_TOLERANCE = 1.0; // 기본 허용 각도 차이 (도)
        private const double MAX_DISTANCE = 300.0; // 같은 그룹으로 처리할 최대 거리
        private const string COMMAND_NAME = "GROUPLINES";

        // AutoCAD 표준 색상 인덱스 배열 (ACI Colors)
        private static readonly int[] GroupColors = [1, 2, 3, 4, 5, 6, 9, 12, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140];
        // Red, Yellow, Green, Cyan, Blue, Magenta, Orange, 등등...

        /// <summary>
        /// 선택된 라인들을 기울기별로 그룹화하고 색상을 적용하는 메인 커맨드
        /// </summary>
        [CommandMethod(COMMAND_NAME)]
        public void GroupLinesBySlope()
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

                // 2단계: 한 번의 Transaction으로 모든 Line 객체 로드 및 그룹화
                using var tr = db.TransactionManager.StartTransaction();

                // ObjectId에서 Line 객체로 직접 변환
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

                // Line 객체들로 그룹화 수행
                var lineGroups = GroupLinesByAngleAndDistance(lines, DEFAULT_TOLERANCE);

                tr.Commit();

                // 3단계: 그룹별 색상 적용 (Handle 기반 매칭)
                ApplyColorsToGroups(lineGroups, selectedLineIds, db);

                // 4단계: 결과 출력
                ed.WriteMessage($"\n{lineGroups.Count}개 그룹으로 분류 완료. 색상이 적용되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 허용 각도 차이를 사용자로부터 입력받는 추가 커맨드
        /// </summary>
        [CommandMethod("GROUPLINES_CUSTOM")]
        public void GroupLinesBySlopeWithCustomTolerance()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 허용 각도 차이 입력
                var tolPrompt = new PromptDoubleOptions($"\n허용 각도 차이를 입력하세요 (기본값: {DEFAULT_TOLERANCE}도): ")
                {
                    DefaultValue = DEFAULT_TOLERANCE,
                    AllowNegative = false,
                    AllowZero = false
                };

                var tolResult = ed.GetDouble(tolPrompt);
                if (tolResult.Status != PromptStatus.OK)
                    return;

                double tolerance = tolResult.Value;

                // 라인 선택 및 그룹화
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 라인이 없습니다.");
                    return;
                }

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

                var lineGroups = GroupLinesByAngleAndDistance(lines, tolerance);

                // 색상 적용 (같은 Transaction 내에서, UpgradeOpen 사용)
                ApplyColorsDirectly(lineGroups);

                tr.Commit();

                //ApplyColorsToGroups(lineGroups, selectedLineIds, db);

                ed.WriteMessage($"\n{lineGroups.Count}개 그룹으로 분류 완료. 색상이 적용되었습니다.");
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
                MessageForAdding = "\n라인들을 선택하세요: "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                lineIds.AddRange(psr.Value.GetObjectIds());
            }

            return lineIds;
        }

        /// <summary>
        /// 라인의 기울기를 도(degree) 단위로 계산
        /// </summary>
        private static double CalculateLineAngle(Line line)
        {
            // Vector2d를 사용하여 시작점과 끝점 사이의 벡터 계산
            var startPt = new Point2d(line.StartPoint.X, line.StartPoint.Y);
            var endPt = new Point2d(line.EndPoint.X, line.EndPoint.Y);

            var vector = startPt.GetVectorTo(endPt);

            // 각도를 라디안에서 도로 변환
            double angleInRadians = vector.Angle;
            double angleInDegrees = angleInRadians * 180.0 / Math.PI;

            // 0~180도 범위로 정규화 (평행선 판단을 위해)
            return NormalizeAngle(angleInDegrees);
        }

        /// <summary>
        /// 두 각도가 평행한지 판단 (허용범위 포함)
        /// </summary>
        private static bool AreAnglesParallel(double angle1, double angle2, double tolerance)
        {
            // 각도를 0~180 범위로 정규화
            angle1 = NormalizeAngle(angle1);
            angle2 = NormalizeAngle(angle2);

            // 각도 차이 계산
            double diff = Math.Abs(angle1 - angle2);

            // 180도 근처에서의 평행선 처리 (예: 179도와 1도)
            if (diff > 90)
                diff = 180 - diff;

            return diff <= tolerance;
        }

        /// <summary>
        /// 각도를 0~180도 범위로 정규화
        /// </summary>
        private static double NormalizeAngle(double angle)
        {
            // 각도를 0~360 범위로 먼저 정규화
            angle = angle % 360;
            if (angle < 0) angle += 360;

            // 180도 이상이면 180을 빼서 0~180 범위로 만듦 (평행선 처리를 위해)
            if (angle >= 180)
                angle -= 180;

            return angle;
        }

        /// <summary>
        /// 두 라인 사이의 최단거리를 계산 (Line 객체 직접 사용)
        /// </summary>
        private static double CalculateMinimumDistanceBetweenLines(Line line1, Line line2)
        {
            try
            {
                // GetClosestPointTo를 사용하여 각 라인에서 상대방 라인의 양 끝점에 대한 최단점을 구하고 거리를 계산
                var distances = new List<double>();

                // line1에서 line2의 시작점과 끝점까지의 거리
                var closestPoint1 = line1.GetClosestPointTo(line2.StartPoint, true);
                distances.Add(closestPoint1.DistanceTo(line2.StartPoint));

                var closestPoint2 = line1.GetClosestPointTo(line2.EndPoint, true);
                distances.Add(closestPoint2.DistanceTo(line2.EndPoint));

                // line2에서 line1의 시작점과 끝점까지의 거리
                var closestPoint3 = line2.GetClosestPointTo(line1.StartPoint, true);
                distances.Add(closestPoint3.DistanceTo(line1.StartPoint));

                var closestPoint4 = line2.GetClosestPointTo(line1.EndPoint, true);
                distances.Add(closestPoint4.DistanceTo(line1.EndPoint));

                return distances.Min();
            }
            catch (System.Exception)
            {
                return double.MaxValue;
            }
        }

        /// <summary>
        /// 두 라인이 같은 그룹에 속할 수 있는지 판단 (Line 객체 직접 사용)
        /// </summary>
        private static bool AreLinesSameGroup(Line line1, Line line2, double angleTolerance)
        {
            try
            {
                // 1단계: 각도가 평행한지 확인 (빠른 필터링)
                double angle1 = CalculateLineAngle(line1);
                double angle2 = CalculateLineAngle(line2);

                if (!AreAnglesParallel(angle1, angle2, angleTolerance))
                    return false;

                // 2단계: 두 라인 사이의 최단거리가 100 이내인지 확인
                double minDistance = CalculateMinimumDistanceBetweenLines(line1, line2);
                return minDistance <= MAX_DISTANCE;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 라인들을 기울기와 거리별로 그룹화 (Line 객체 직접 사용)
        /// </summary>
        private List<List<Line>> GroupLinesByAngleAndDistance(List<Line> lines, double tolerance)
        {
            var groups = new List<List<Line>>();
            var remainingLines = new List<Line>(lines);

            while (remainingLines.Count > 0)
            {
                var currentLine = remainingLines[0];
                var currentGroup = new List<Line> { currentLine };
                remainingLines.RemoveAt(0);

                // 현재 라인과 평행하고 거리 조건(100mm 이내)을 만족하는 라인들을 찾아서 그룹에 추가
                for (int i = remainingLines.Count - 1; i >= 0; i--)
                {
                    var compareLine = remainingLines[i];

                    // 각도 조건 AND 거리 조건을 모두 만족해야 같은 그룹
                    if (AreLinesSameGroup(currentLine, compareLine, tolerance))
                    {
                        currentGroup.Add(compareLine);
                        remainingLines.RemoveAt(i);
                    }
                }

                // 그룹을 각도순으로 정렬
                currentGroup.Sort((line1, line2) =>
                {
                    double angle1 = CalculateLineAngle(line1);
                    double angle2 = CalculateLineAngle(line2);
                    return angle1.CompareTo(angle2);
                });

                groups.Add(currentGroup);
            }

            // 그룹들을 크기 순으로 정렬 (큰 그룹부터)
            groups.Sort((a, b) => b.Count.CompareTo(a.Count));
            return groups;
        }

        /// <summary>
        /// 그룹별로 라인에 색상을 적용 (Handle 기반 매칭)
        /// </summary>
        private void ApplyColorsToGroups(List<List<Line>> lineGroups, List<ObjectId> originalLineIds, Database db)
        {
            using var tr = db.TransactionManager.StartTransaction();

            for (int groupIndex = 0; groupIndex < lineGroups.Count; groupIndex++)
            {
                var lineGroup = lineGroups[groupIndex];
                int colorIndex = GroupColors[groupIndex % GroupColors.Length];
                var color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);

                foreach (var line in lineGroup)
                {
                    try
                    {
                        // Line의 Handle로 원본 ObjectId 찾기
                        var matchingId = originalLineIds.FirstOrDefault(id => id.Handle == line.Handle);
                        if (matchingId != ObjectId.Null)
                        {
                            if (tr.GetObject(matchingId, OpenMode.ForWrite) is Line writableLine)
                            {
                                writableLine.Color = color;
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // 개별 라인 색상 적용 실패 시 무시하고 계속 진행
                        continue;
                    }
                }
            }

            tr.Commit();
        }

        private void ApplyColorsDirectly(List<List<Line>> lineGroups)
        {
            for (int i = 0; i < lineGroups.Count; i++)
            {
                var color = Color.FromColorIndex(ColorMethod.ByAci, (short)GroupColors[i % GroupColors.Length]);

                foreach (var line in lineGroups[i])
                {
                    try
                    {
                        line.UpgradeOpen();  // ForRead → ForWrite 업그레이드
                        line.Color = color;   // 직접 색상 수정!
                    }
                    catch (System.Exception)
                    {
                        continue; // 실패 시 무시하고 계속
                    }
                }
            }
        }

        /// <summary>
        /// 그룹별 통계 정보 출력 (Line 객체 직접 사용)
        /// </summary>
        [CommandMethod("GROUPLINES_STATS")]
        public void ShowGroupStatistics()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 라인이 없습니다.");
                    return;
                }

                using var tr = db.TransactionManager.StartTransaction();

                var lines = selectedLineIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(line => line != null)
                    .ToList();

                var lineGroups = GroupLinesByAngleAndDistance(lines, DEFAULT_TOLERANCE);

                tr.Commit();

                ed.WriteMessage($"\n=== 그룹 통계 정보 ===");
                ed.WriteMessage($"\n총 {lineGroups.Count}개 그룹이 생성되었습니다.");
                ed.WriteMessage($"\n총 {selectedLineIds.Count}개의 라인이 처리되었습니다.\n");

                for (int i = 0; i < lineGroups.Count; i++)
                {
                    var group = lineGroups[i];
                    if (group.Count > 0)
                    {
                        var firstLine = group[0];
                        double firstAngle = CalculateLineAngle(firstLine);

                        // 그룹 내 평균 길이 계산 (Line 객체에서 직접)
                        double avgLength = group.Average(line => line.StartPoint.DistanceTo(line.EndPoint));

                        ed.WriteMessage($"\n그룹 {i + 1}: {group.Count}개 라인");
                        ed.WriteMessage($"  - 각도: {firstAngle:F1}도");
                        ed.WriteMessage($"  - 평균 길이: {avgLength:F2}");
                        ed.WriteMessage($"  - 색상 인덱스: {GroupColors[i % GroupColors.Length]}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }



    public class CenterLine
    {
        //1. 가장 먼 2점 p1, p2 찾기 ✅ 
        //2. p1 → 상대선에서 수직 투영점 np1 찾기(GetClosestPointTo) ✅
        //3. p1과 np1의 중간점 pp1 계산 ✅
        //4. p2 → 상대선에서 수직 투영점 np2 찾기 ✅
        //5. p2와 np2의 중간점 pp2 계산 ✅  
        //6. pp1과 pp2를 잇는 중간선 생성 ✅


        [CommandMethod("CreateMiddleLine")]
        public void CreateMiddleLine()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: 두 개의 Line 객체를 선택
                Line line1 = SelectLine(ed, "\n첫 번째 평행선을 선택하세요: ");
                if (line1 == null) return;

                Line line2 = SelectLine(ed, "\n두 번째 평행선을 선택하세요: ");
                if (line2 == null) return;

                // Step 2: 두 선이 평행한지 확인 (1도 이내)
                double angle = GetAngleBetweenLines(line1, line2);
                if (!AreNearlyParallel(line1, line2, 1.0))
                {
                    ed.WriteMessage($"\n선택한 두 선이 평행하지 않습니다. 현재 각도: {angle:F2}도 (1도 이내 허용)");
                    return;
                }
                else
                {
                    ed.WriteMessage($"\n두 선의 각도: {angle:F2}도 - 평행 조건 만족");
                }

                // Step 3: 중간선 생성 (4개 점 중 가장 먼 2개 점 기준)
                var result = CreateMiddleLineFromParallelsWithInfo(line1, line2);
                Line middleLine = result.line;
                double maxDistance = result.maxDistance;

                // Step 4: 중간선을 데이터베이스에 추가
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;

                    btr.AppendEntity(middleLine);
                    tr.AddNewlyCreatedDBObject(middleLine, true);

                    tr.Commit();
                    ed.WriteMessage($"\n중간선이 생성되었습니다.");
                    ed.WriteMessage($"\n - 중간선 길이: {middleLine.Length:F3}");
                    ed.WriteMessage($"\n - 기준 거리 (가장 먼 두 점): {maxDistance:F3}");
                    ed.WriteMessage($"\n - 두 선의 각도: {angle:F2}도");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        private Line SelectLine(Editor ed, string prompt)
        {
            PromptEntityOptions peo = new PromptEntityOptions(prompt);
            peo.SetRejectMessage("\nLine 객체만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return null;

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                Line line = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
                tr.Commit();
                return line;
            }
        }

        private double GetAngleBetweenLines(Line line1, Line line2)
        {
            // 두 선의 방향 벡터 계산
            Vector3d dir1 = line1.EndPoint - line1.StartPoint;
            Vector3d dir2 = line2.EndPoint - line2.StartPoint;

            // 벡터를 단위벡터로 정규화
            dir1 = dir1.GetNormal();
            dir2 = dir2.GetNormal();

            // 두 벡터 사이의 각도 계산 (내적 사용)
            double dotProduct = dir1.DotProduct(dir2);

            // 내적의 절댓값을 사용 (방향이 반대일 수도 있음)
            dotProduct = Math.Abs(dotProduct);

            // 각도 계산 (라디안)
            double angleRad = Math.Acos(Math.Min(1.0, dotProduct)); // Math.Min으로 부동소수점 오차 방지

            // 라디안을 도로 변환
            double angleDeg = angleRad * 180.0 / Math.PI;

            return angleDeg;
        }

        private bool AreNearlyParallel(Line line1, Line line2, double toleranceDegrees = 1.0)
        {
            double angle = GetAngleBetweenLines(line1, line2);
            return angle <= toleranceDegrees;
        }

        private (Line line, double maxDistance) CreateMiddleLineFromParallelsWithInfo(Line line1, Line line2)
        {
            // 두 선의 모든 점들을 배열로 정리
            Point3d[] allPoints = {
                line1.StartPoint,
                line1.EndPoint,
                line2.StartPoint,
                line2.EndPoint
            };

            // 가장 먼 두 점을 찾기
            var farthestPoints = FindFarthestTwoPoints(allPoints);
            Point3d p1 = farthestPoints.point1;
            Point3d p2 = farthestPoints.point2;
            double maxDistance = farthestPoints.distance;

            // p1이 어느 선에 속하는지 확인하고 상대 선 찾기
            Line oppositeLine1 = IsPointOnLine(p1, line1) ? line2 : line1;
            Point3d np1 = oppositeLine1.GetClosestPointTo(p1, true);
            Point3d pp1 = GetMidpoint(p1, np1);

            // p2가 어느 선에 속하는지 확인하고 상대 선 찾기  
            Line oppositeLine2 = IsPointOnLine(p2, line1) ? line2 : line1;
            Point3d np2 = oppositeLine2.GetClosestPointTo(p2, true);
            Point3d pp2 = GetMidpoint(p2, np2);

            // pp1과 pp2를 잇는 중간선 생성
            Line middleLine = new Line(pp1, pp2);
            return (middleLine, maxDistance);
        }

        private bool IsPointOnLine(Point3d point, Line line)
        {
            // 점이 선 위에 있는지 확인 (허용 오차 포함)
            double tolerance = 1e-6;
            Point3d closestPoint = line.GetClosestPointTo(point, true);
            double distance = point.DistanceTo(closestPoint);
            return distance < tolerance;
        }

        private (Point3d point1, Point3d point2, double distance) FindFarthestTwoPoints(Point3d[] points)
        {
            double maxDistance = 0.0;
            Point3d farthestPoint1 = points[0];
            Point3d farthestPoint2 = points[1];

            // 모든 점들의 조합을 확인하여 가장 먼 거리 찾기
            for (int i = 0; i < points.Length - 1; i++)
            {
                for (int j = i + 1; j < points.Length; j++)
                {
                    double distance = points[i].DistanceTo(points[j]);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        farthestPoint1 = points[i];
                        farthestPoint2 = points[j];
                    }
                }
            }

            return (farthestPoint1, farthestPoint2, maxDistance);
        }

        private Point3d GetMidpoint(Point3d point1, Point3d point2)
        {
            return new Point3d(
                (point1.X + point2.X) / 2.0,
                (point1.Y + point2.Y) / 2.0,
                (point1.Z + point2.Z) / 2.0
            );
        }
    }

}
