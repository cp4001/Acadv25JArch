using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCADPlugin
{
    public class ClockwiseLineSelector
    {
        private const double TOLERANCE = 1e-6; // 접촉점 판단 허용 오차

        /// <summary>
        /// L1 기준으로 시계방향 각도가 가장 작은 라인을 반환
        /// </summary>
        /// <param name="lines">라인 리스트 (첫 번째가 기준 라인 L1, 나머지는 L2~Ln)</param>
        /// <returns>시계방향 각도가 가장 작은 라인, 없으면 null</returns>
        public static Line FindMinimumClockwiseAngleLine(List<Line> lines)
        {
            // 입력 검증
            if (lines == null || lines.Count < 2)
                return null;

            // 기준 라인 L1
            Line baseLine = lines[0];
            
            // 후보 라인들 (L2 ~ Ln)
            var candidateLines = lines.Skip(1).ToList();

            // Step 1: L1의 접촉점 찾기 (다른 라인들이 어느 끝점에 접하는지)
            Point3d connectionPoint = FindConnectionPoint(baseLine, candidateLines);
            if (connectionPoint == Point3d.Origin) // 접촉점을 찾지 못한 경우
                return null;

            // Step 2: L1의 방향벡터 결정 (접촉점에서 바깥쪽을 향하도록)
            // 아래쪽 수평의 경우 값이 큰 쪽이 start 기점 
            Vector2d baseDirection = GetBaseDirection(baseLine, connectionPoint);

            // Step 3: 각 후보 라인의 시계방향 각도 계산
            var lineAngles = new List<(Line line, double angle)>();

            foreach (var candidateLine in candidateLines)
            {
                // 후보 라인이 접촉점과 연결되어 있는지 확인
                if (!IsLineConnectedToPoint(candidateLine, connectionPoint, TOLERANCE))
                    continue;

                // 후보 라인의 방향벡터 계산 (접촉점에서 바깥쪽으로)
                Vector2d candidateDirection = GetDirectionFromPoint(candidateLine, connectionPoint);

                // 시계방향 각도 계산
                double clockwiseAngle = CalculateClockwiseAngle(baseDirection, candidateDirection);

                lineAngles.Add((candidateLine, clockwiseAngle));
            }

            // Step 4: 가장 작은 각도의 라인 반환
            if (lineAngles.Count == 0)
                return null;

            var result = lineAngles.OrderBy(x => x.angle).First();
            return result.line;
        }

        /// <summary>
        /// L1의 접촉점 찾기 (다른 라인들이 접하는 점)
        /// </summary>
        private static Point3d FindConnectionPoint(Line baseLine, List<Line> candidateLines)
        {
            // L1의 StartPoint에 접하는 라인 개수
            int startPointCount = candidateLines.Count(line => 
                IsLineConnectedToPoint(line, baseLine.StartPoint, TOLERANCE));

            // L1의 EndPoint에 접하는 라인 개수
            int endPointCount = candidateLines.Count(line => 
                IsLineConnectedToPoint(line, baseLine.EndPoint, TOLERANCE));

            // 더 많은 라인이 접하는 점을 접촉점으로 선택
            if (startPointCount > endPointCount)
                return baseLine.StartPoint;
            else if (endPointCount > 0)
                return baseLine.EndPoint;
            else
                return Point3d.Origin; // 접촉점을 찾지 못함
        }

        /// <summary>
        /// 라인이 특정 점과 연결되어 있는지 확인
        /// </summary>
        private static bool IsLineConnectedToPoint(Line line, Point3d point, double tolerance)
        {
            double distToStart = line.StartPoint.DistanceTo(point);
            double distToEnd = line.EndPoint.DistanceTo(point);

            return distToStart < tolerance || distToEnd < tolerance;
        }

        /// <summary>
        /// L1의 방향벡터 계산 (접촉점에서 바깥쪽으로)
        /// </summary>
        private static Vector2d GetBaseDirection(Line baseLine, Point3d connectionPoint)
        {
            // 접촉점이 StartPoint면 StartPoint → EndPoint 방향
            // 접촉점이 EndPoint면 EndPoint → StartPoint 방향
            
            double distToStart = baseLine.StartPoint.DistanceTo(connectionPoint);
            
            Point3d fromPoint, toPoint;
            
            if (distToStart < TOLERANCE) // 접촉점이 StartPoint
            {
                fromPoint = baseLine.StartPoint;
                toPoint = baseLine.EndPoint;
            }
            else // 접촉점이 EndPoint
            {
                fromPoint = baseLine.EndPoint;
                toPoint = baseLine.StartPoint;
            }

            // 2D 방향벡터 계산
            double dx = toPoint.X - fromPoint.X;
            double dy = toPoint.Y - fromPoint.Y;

            return new Vector2d(dx, dy);
        }

        /// <summary>
        /// 후보 라인의 방향벡터 계산 (접촉점에서 바깥쪽으로)
        /// </summary>
        private static Vector2d GetDirectionFromPoint(Line line, Point3d connectionPoint)
        {
            double distToStart = line.StartPoint.DistanceTo(connectionPoint);
            
            Point3d fromPoint, toPoint;
            
            if (distToStart < TOLERANCE) // 접촉점이 StartPoint
            {
                fromPoint = line.StartPoint;
                toPoint = line.EndPoint;
            }
            else // 접촉점이 EndPoint
            {
                fromPoint = line.EndPoint;
                toPoint = line.StartPoint;
            }

            // 2D 방향벡터 계산
            double dx = toPoint.X - fromPoint.X;
            double dy = toPoint.Y - fromPoint.Y;

            return new Vector2d(dx, dy);
        }

        /// <summary>
        /// Math.Atan2를 사용하여 시계방향 각도 계산
        /// </summary>
        /// <param name="baseVector">기준 벡터 (L1의 방향)</param>
        /// <param name="targetVector">대상 벡터 (후보 라인의 방향)</param>
        /// <returns>시계방향 각도 (0~360도)</returns>
        private static double CalculateClockwiseAngle(Vector2d baseVector, Vector2d targetVector)
        {
            // 기준 벡터의 각도 계산 (반시계방향, -180 ~ 180도)
            double baseAngleRad = Math.Atan2(baseVector.Y, baseVector.X);
            
            // 대상 벡터의 각도 계산 (반시계방향, -180 ~ 180도)
            double targetAngleRad = Math.Atan2(targetVector.Y, targetVector.X);

            // 라디안을 도로 변환
            double baseAngleDeg = baseAngleRad * 180.0 / Math.PI;
            double targetAngleDeg = targetAngleRad * 180.0 / Math.PI;

            // 상대 각도 계산 (반시계방향)
            double relativeAngle = targetAngleDeg - baseAngleDeg;

            // 시계방향 각도로 변환
            // 반시계방향이 양수이므로, 시계방향은 음수로 변환
            double clockwiseAngle = -relativeAngle;

            // 0~360도 범위로 정규화
            while (clockwiseAngle < 0)
                clockwiseAngle += 360;
            while (clockwiseAngle >= 360)
                clockwiseAngle -= 360;

            return clockwiseAngle;
        }

        /// <summary>
        /// 테스트용 커맨드: 라인 선택 후 시계방향 각도가 가장 작은 라인 강조
        /// </summary>
        [CommandMethod("FINDMINANGLE")]
        public void FindMinimumAngleLine()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 라인들 선택
                var selectedLines = SelectMultipleLines(ed);
                if (selectedLines.Count < 2)
                {
                    ed.WriteMessage("\n최소 2개 이상의 라인을 선택해야 합니다.");
                    return;
                }

                // 시계방향 각도가 가장 작은 라인 찾기
                Line resultLine = FindMinimumClockwiseAngleLine(selectedLines);

                if (resultLine == null)
                {
                    ed.WriteMessage("\n조건을 만족하는 라인을 찾을 수 없습니다.");
                    return;
                }

                // 결과 라인을 빨간색으로 강조
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 결과 라인의 Handle로 ObjectId 찾기
                    foreach (var line in selectedLines)
                    {
                        if (line.Handle == resultLine.Handle)
                        {
                            // 데이터베이스에서 다시 열기
                            var dbObj = tr.GetObject(db.GetObjectId(false, line.Handle, 0), OpenMode.ForWrite);
                            if (dbObj is Line writableLine)
                            {
                                writableLine.ColorIndex = 1; // 빨간색
                                ed.WriteMessage($"\n가장 작은 시계방향 각도의 라인을 빨간색으로 표시했습니다.");
                            }
                            break;
                        }
                    }

                    tr.Commit();
                }

                // 각도 정보 출력
                DisplayAngleInformation(selectedLines, ed);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 여러 라인 선택
        /// </summary>
        private List<Line> SelectMultipleLines(Editor ed)
        {
            var lines = new List<Line>();

            TypedValue[] filterList = [new TypedValue((int)DxfCode.Start, "LINE")];
            var filter = new SelectionFilter(filterList);

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = "\n라인들을 선택하세요 (첫 번째가 기준 라인 L1): "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId objId in psr.Value.GetObjectIds())
                    {
                        if (tr.GetObject(objId, OpenMode.ForRead) is Line line)
                        {
                            lines.Add(line);
                        }
                    }
                    tr.Commit();
                }
            }

            return lines;
        }

        /// <summary>
        /// 각도 정보 출력 (디버깅용)
        /// </summary>
        private void DisplayAngleInformation(List<Line> lines, Editor ed)
        {
            if (lines.Count < 2) return;

            Line baseLine = lines[0];
            var candidateLines = lines.Skip(1).ToList();

            Point3d connectionPoint = FindConnectionPoint(baseLine, candidateLines);
            if (connectionPoint == Point3d.Origin)
            {
                ed.WriteMessage("\n접촉점을 찾을 수 없습니다.");
                return;
            }

            Vector2d baseDirection = GetBaseDirection(baseLine, connectionPoint);

            ed.WriteMessage("\n\n=== 시계방향 각도 정보 ===");
            ed.WriteMessage($"\n접촉점: ({connectionPoint.X:F2}, {connectionPoint.Y:F2})");
            ed.WriteMessage($"\n기준 라인 방향: ({baseDirection.X:F2}, {baseDirection.Y:F2})");

            int index = 2;
            foreach (var candidateLine in candidateLines)
            {
                if (!IsLineConnectedToPoint(candidateLine, connectionPoint, TOLERANCE))
                {
                    ed.WriteMessage($"\nL{index}: 접촉점과 연결되지 않음");
                    index++;
                    continue;
                }

                Vector2d candidateDirection = GetDirectionFromPoint(candidateLine, connectionPoint);
                double angle = CalculateClockwiseAngle(baseDirection, candidateDirection);

                ed.WriteMessage($"\nL{index}: 시계방향 각도 = {angle:F2}도");
                index++;
            }
        }
    }
}
