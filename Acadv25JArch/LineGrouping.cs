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
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;

namespace Acadv25JArch
{
    public class LineGroupingCommand
    {
        // .NET 8.0 기능: 컴파일 타임 상수
        private const double DEFAULT_TOLERANCE = 1.0; // 기본 허용 각도 차이 (도)
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
                var selectedLines = SelectLines(ed);
                if (selectedLines.Count == 0)
                {
                    ed.WriteMessage("\n선택된 라인이 없습니다.");
                    return;
                }

                // 2단계: 라인 정보 수집 (기울기 계산)
                var lineInfos = GetLineInformation(selectedLines, db);
                if (lineInfos.Count == 0)
                {
                    ed.WriteMessage("\n유효한 라인 정보를 가져올 수 없습니다.");
                    return;
                }

                // 3단계: 기울기별 그룹화
                var groups = GroupLinesByAngle(lineInfos, DEFAULT_TOLERANCE);

                // 4단계: 그룹별 색상 적용
                ApplyColorsToGroups(groups, db);

                // 5단계: 간단한 결과 출력
                ed.WriteMessage($"\n{groups.Count}개 그룹으로 분류 완료. 색상이 적용되었습니다.");
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
                var selectedLines = SelectLines(ed);
                if (selectedLines.Count == 0)
                {
                    ed.WriteMessage("\n선택된 라인이 없습니다.");
                    return;
                }

                var lineInfos = GetLineInformation(selectedLines, db);
                var groups = GroupLinesByAngle(lineInfos, tolerance);

                // 그룹별 색상 적용
                ApplyColorsToGroups(groups, db);

                ed.WriteMessage($"\n{groups.Count}개 그룹으로 분류 완료. 색상이 적용되었습니다.");
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
        /// 라인 정보 수집 (기울기 계산 포함)
        /// </summary>
        private List<LineInfo> GetLineInformation(List<ObjectId> lineIds, Database db)
        {
            var lineInfos = new List<LineInfo>();

            using var tr = db.TransactionManager.StartTransaction();

            foreach (var lineId in lineIds)
            {
                try
                {
                    if (tr.GetObject(lineId, OpenMode.ForRead) is Line line)
                    {
                        double angle = CalculateLineAngle(line);
                        lineInfos.Add(new LineInfo(lineId, line.StartPoint, line.EndPoint, angle));
                    }
                }
                catch (System.Exception ex)
                {
                    // 개별 라인 처리 실패 시 로그만 남기고 계속 진행
                    Application.DocumentManager.MdiActiveDocument.Editor
                        .WriteMessage($"\n라인 {lineId.Handle} 처리 중 오류: {ex.Message}");
                }
            }

            tr.Commit();
            return lineInfos;
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
        /// 30도와 210도는 평행한 것으로 처리 (180도 차이)
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
        /// 라인들을 기울기별로 그룹화
        /// </summary>
        private List<List<LineInfo>> GroupLinesByAngle(List<LineInfo> lineInfos, double tolerance)
        {
            var groups = new List<List<LineInfo>>();
            var remainingLines = new List<LineInfo>(lineInfos);

            while (remainingLines.Count > 0)
            {
                var currentLine = remainingLines[0];
                var currentGroup = new List<LineInfo> { currentLine };
                remainingLines.RemoveAt(0);

                // 현재 라인과 평행한 라인들을 찾아서 그룹에 추가
                for (int i = remainingLines.Count - 1; i >= 0; i--)
                {
                    if (AreAnglesParallel(currentLine.Angle, remainingLines[i].Angle, tolerance))
                    {
                        currentGroup.Add(remainingLines[i]);
                        remainingLines.RemoveAt(i);
                    }
                }

                // 그룹을 각도순으로 정렬
                currentGroup.Sort((a, b) => a.Angle.CompareTo(b.Angle));
                groups.Add(currentGroup);
            }

            // 그룹들을 크기 순으로 정렬 (큰 그룹부터)
            groups.Sort((a, b) => b.Count.CompareTo(a.Count));
            return groups;
        }

        /// <summary>
        /// 그룹별로 라인에 색상을 적용
        /// </summary>
        private void ApplyColorsToGroups(List<List<LineInfo>> groups, Database db)
        {
            using var tr = db.TransactionManager.StartTransaction();

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var group = groups[groupIndex];
                int colorIndex = GroupColors[groupIndex % GroupColors.Length];
                var color = Color.FromColorIndex(ColorMethod.ByAci, (short)colorIndex);

                foreach (var lineInfo in group)
                {
                    try
                    {
                        if (tr.GetObject(lineInfo.ObjectId, OpenMode.ForWrite) is Line line)
                        {
                            line.Color = color;
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
    }

    /// <summary>
    /// 라인 정보를 저장하는 레코드 (.NET 8.0의 record 기능 사용)
    /// </summary>
    public record LineInfo(ObjectId ObjectId, Point3d StartPoint, Point3d EndPoint, double Angle)
    {
        /// <summary>
        /// 라인의 길이 계산
        /// </summary>
        public double Length => StartPoint.DistanceTo(EndPoint);

        /// <summary>
        /// 라인의 중점 계산
        /// </summary>
        public Point3d MidPoint => new Point3d(
            (StartPoint.X + EndPoint.X) / 2,
            (StartPoint.Y + EndPoint.Y) / 2,
            (StartPoint.Z + EndPoint.Z) / 2
        );
    }
}
