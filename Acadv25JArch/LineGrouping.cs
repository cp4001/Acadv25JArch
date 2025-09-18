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

using CADExtension;

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
        [CommandMethod("Group_Lines")]
        public void Cmd_GroupLinesBySlope()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 라인들 선택
                //var selectedLineIds = SelectLines(ed);
                var selectedLineIds = SelectLinesCurrentLayer(ed);  
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

        [CommandMethod("Group_Lines11")]
        public void Cmd_GroupLinesBySlope1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 라인들 선택
                //var selectedLineIds = SelectLines(ed);
                var selectedLineIds = SelectLinesCurrentLayer(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 라인이 없습니다.");
                    return;
                }


                // 2단계: 기준점 선택
                PromptPointOptions pointOpts = new PromptPointOptions("\n기준점을 선택하세요: ");
                pointOpts.AllowNone = false;

                PromptPointResult pointResult = ed.GetPoint(pointOpts);
                if (pointResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n작업이 취소되었습니다.");
                    return;
                }

                Point3d basePoint = pointResult.Value;


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

                // 기준점에서 가까운 순서로 정렬
                //lines = lines.OrderBy(line => line.GetClosestPointTo(basePoint, false).DistanceTo(basePoint)).ToList();

                //  기준점에서 line 센터까지의 거리로 정열
                lines = lines.OrderBy(line => line.GetCenDisTance(basePoint)).ToList();

                // line 길이가 250 보다 적으면 제외
                lines = lines.Where(line => line.Length  > 250.0).ToList();


                // 기준점 대비 line 각도 적은 것은 제외
                //lines = lines.Where(x=> x.GetAngleSpEp(basePoint) > 3 ).ToList();  



                //// Line 객체들로 그룹화 수행
                var lineGroups = GroupLinesByAngleAndDistance(lines,basePoint, DEFAULT_TOLERANCE);

                //// Group의 첫번쨰 만 추출
                var firstLines = lineGroups.Select(g => g.First()).ToList();

                // 색상 적용 (같은 Transaction 내에서, UpgradeOpen 사용)
                // 빨간색으로 변경

                //line에서 8개만 선택     
                var slines = firstLines.Take(5).ToList();

                Color redColor = Color.FromColorIndex(ColorMethod.ByAci, 1);

                foreach (var ll in slines)
                {
                    try
                    {
                        // 쓰기 모드로 업그레이드하여 색상 변경
                        ll.UpgradeOpen();
                        ll.Color = redColor;
                        ll.DowngradeOpen();

                    }
                    catch (System.Exception)
                    {
                        // 개별 선분 처리 실패 시 계속 진행
                        continue;
                    }
                }

                tr.Commit();

                //// 3단계: 그룹별 색상 적용 (Handle 기반 매칭)
                //ApplyColorsToGroups(lineGroups, selectedLineIds, db);

                // 4단계: 결과 출력
                //ed.WriteMessage($"\n{lineGroups.Count}개 그룹으로 분류 완료. 색상이 적용되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// 선택된 라인들을 기울기별로 그룹화하고 대상 그룹에 센터 라인을 생성하는 메인 커맨드
        /// </summary>
        [CommandMethod("Group_Lines_And_Middle_Lines")]
        public void Cmd_GroupLinesBySlopeAndMiddleLine()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 라인들 선택
                //var selectedLineIds = SelectLines(ed);
                var selectedLineIds = SelectLinesCurrentLayer(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 라인이 없습니다.");
                    return;
                }

                // 2단계: 한 번의 Transaction으로 모든 Line 객체 로드 및 그룹화
                using var tr = db.TransactionManager.StartTransaction();

                // ObjectId에서 Entity 객체로 직접 변환
                var ents = selectedLineIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Entity)
                    .Where(ent => ent != null)
                    .ToList();

                // ObjectId에서 Line 객체로 직접 변환
                List<Line> lines = new List<Line>();

                foreach (var ent in ents)
                {
                    if(ent.GetType() == typeof(Line)) lines.Add(ent as Line);
                    if(ent.GetType() == typeof(Polyline))
                    {
                        Polyline pl = ent as Polyline;
                        lines.AddRange(pl.GetLineEntities());
                    }
                }

                //var lines = selectedLineIds
                //    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                //    .Where(line => line != null)
                //    .ToList();

                if (lines.Count == 0)
                {
                    ed.WriteMessage("\n유효한 라인을 로드할 수 없습니다.");
                    tr.Commit();
                    return;
                }

                // Line 객체들로 그룹화 수행
                // line 을 긴것부터 우선 처리 되게 정열
                lines = lines.OrderByDescending(line => line.Length).ToList();
                var lineGroups = GroupLinesByAngleAndDistance(lines, DEFAULT_TOLERANCE);

                // 3단계: 그룹별 센터 line 생성 
                //ApplyColorsToGroups(lineGroups, selectedLineIds, db);
                //BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                //BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite) as BlockTableRecord;
                var btr = tr.GetModelSpaceBlockTableRecord(db);

                
                foreach (var group in lineGroups)
                {
                    if (group.Count >= 2) // 그룹에 2개 이상의 라인이 있을 때만 센터 라인 생성
                    {
                        // 길이 기준 오름차순 (긴 것부터)
                        var group1  = group.OrderByDescending(line => line.Length).ToList();
                        var centerLineCreator = new CenterLine();
                        var result = centerLineCreator.CreateMiddleLineFromParallelsWithInfo(group1[0], group1[1]);
                        Line middleLine = result.line;
                        middleLine.Color = Color.FromColorIndex(ColorMethod.ByAci, 1); // Red
                            btr.AppendEntity(middleLine);
                            tr.AddNewlyCreatedDBObject(middleLine, true);
                            //tr.Commit();
                        
                    }
                }

                tr.Commit();

 



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
        /// 현재 레이어 이름을 가져오는 메서드
        /// </summary>
        private string GetCurrentLayerName(Database db)
        {
            using var tr = db.TransactionManager.StartTransaction();

            // 현재 레이어의 ObjectId 가져오기
            ObjectId currentLayerId = db.Clayer;

            // LayerTableRecord로 변환하여 레이어 이름 가져오기
            if (tr.GetObject(currentLayerId, OpenMode.ForRead) is LayerTableRecord currentLayer)
            {
                string layerName = currentLayer.Name;
                tr.Commit();
                return layerName;
            }

            tr.Commit();
            return "0"; // 기본 레이어
        }

        private List<ObjectId> SelectLinesCurrentLayer(Editor ed)
        {
            var linePolyIds = new List<ObjectId>();
            Database db = ed.Document.Database;
            string currentLayerName = GetCurrentLayerName(db);

            // 라인만 선택하도록 필터 설정
            TypedValue[] filterList = [
                new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE,POLYLINE"),
                new TypedValue((int)DxfCode.LayerName, currentLayerName)
            ];
            var filter = new SelectionFilter(filterList);

            var opts = new PromptSelectionOptions
            {
                MessageForAdding = "\n현재 레이어의 Line 또는 Poly 들을 선택하세요: "
            };

            var psr = ed.GetSelection(opts, filter);

            if (psr.Status == PromptStatus.OK && psr.Value != null)
            {
                linePolyIds.AddRange(psr.Value.GetObjectIds());
            }

            return linePolyIds;
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
                // Line1 최소 한점이  Line2에 수직투영했을때  내부에 존재 하는지 확인 
                if(!ParallelLineChecker.HasVerticalProjectionInside(line1, line2))
                {
                    return false;
                }   

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
        public  static List<List<Line>> GroupLinesByAngleAndDistance(List<Line> lines, double tolerance)
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

        //기준점 추가 버전 
        public static List<List<Line>> GroupLinesByAngleAndDistance(List<Line> lines,Point3d pt, double tolerance)
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
                    currentGroup = currentGroup.OrderBy(line => line.ShortestDistanceToPoint(pt)).ToList();

                }

                //// 그룹을 각도순으로 정렬
                //currentGroup.Sort((line1, line2) =>
                //{
                //    double angle1 = CalculateLineAngle(line1);
                //    double angle2 = CalculateLineAngle(line2);
                //    return angle1.CompareTo(angle2);
                //});

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
                if (lineGroup.Count == 1) continue;
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
        public void Cmd_CreateMiddleLine()
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

        public  (Line line, double maxDistance) CreateMiddleLineFromParallelsWithInfo(Line line1, Line line2)
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


    // Colinear line 처리 
    public class ColinearLineProcessor
    {
        // .NET 8.0 기능: 컴파일 타임 상수
        private const double ANGLE_TOLERANCE = 1.0; // 평행 판단용 허용 각도 차이 (도)
        private const double DISTANCE_TOLERANCE = 1e-6; // colinear 판단용 허용 거리 차이

        /// <summary>
        /// colinear한 line들 중에서 가장 큰 line만 남기고 나머지는 제거하는 메인 함수
        /// </summary>
        /// <param name="lines">처리할 Line 객체들의 리스트</param>
        /// <returns>colinear 그룹별로 가장 긴 line만 포함된 새로운 리스트</returns>
        public static List<Line> RemoveRedundantColinearLines(List<Line> lines)
        {
            if (lines == null || lines.Count <= 1)
                return new List<Line>(lines ?? new List<Line>());

            var result = new List<Line>();
            var processedLines = new HashSet<Line>();

            foreach (var line in lines)
            {
                if (processedLines.Contains(line))
                    continue;

                // 현재 line과 colinear한 모든 line들을 찾기
                var colinearGroup = FindColinearLines(line, lines, processedLines);

                // 그룹에서 가장 긴 line 찾기
                var longestLine = FindLongestLine(colinearGroup);
                result.Add(longestLine);

                // 처리된 line들을 표시
                foreach (var processedLine in colinearGroup)
                {
                    processedLines.Add(processedLine);
                }
            }

            return result;
        }

        /// <summary>
        /// 주어진 line과 colinear한 모든 line들을 찾아 그룹으로 반환
        /// </summary>
        private static List<Line> FindColinearLines(Line baseLine, List<Line> allLines, HashSet<Line> processedLines)
        {
            var colinearGroup = new List<Line> { baseLine };

            foreach (var otherLine in allLines)
            {
                if (otherLine == baseLine || processedLines.Contains(otherLine))
                    continue;

                if (AreColinear(baseLine, otherLine))
                {
                    colinearGroup.Add(otherLine);
                }
            }

            return colinearGroup;
        }

        /// <summary>
        /// 두 Line이 colinear한지 판단하는 함수
        /// colinear 조건: 1) 평행해야 함, 2) 한 line의 점이 다른 line의 연장선 위에 있어야 함
        /// </summary>
        private static bool AreColinear(Line line1, Line line2)
        {
            try
            {
                // 1단계: 두 line이 평행한지 확인
                if (!AreParallel(line1, line2))
                    return false;

                // 2단계: 한 line의 점이 다른 line의 연장선 위에 있는지 확인
                // line1의 시작점이 line2의 연장선 위에 있는지 확인
                Point3d closestPoint1 = line2.GetClosestPointTo(line1.StartPoint, true); // true = 연장선 포함
                double distance1 = line1.StartPoint.DistanceTo(closestPoint1);

                if (distance1 <= DISTANCE_TOLERANCE)
                    return true;

                // line1의 끝점이 line2의 연장선 위에 있는지 확인
                Point3d closestPoint2 = line2.GetClosestPointTo(line1.EndPoint, true);
                double distance2 = line1.EndPoint.DistanceTo(closestPoint2);

                return distance2 <= DISTANCE_TOLERANCE;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 두 Line이 평행한지 판단하는 함수
        /// </summary>
        private static bool AreParallel(Line line1, Line line2)
        {
            try
            {
                double angle1 = CalculateLineAngle(line1);
                double angle2 = CalculateLineAngle(line2);
                return AreAnglesParallel(angle1, angle2, ANGLE_TOLERANCE);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Line의 기울기를 도(degree) 단위로 계산
        /// 첨부 샘플 코드의 CalculateLineAngle 함수와 동일한 방식
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
        /// 첨부 샘플 코드의 AreAnglesParallel 함수와 동일
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
        /// 첨부 샘플 코드의 NormalizeAngle 함수와 동일
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
        /// Line 그룹에서 가장 긴 Line을 찾아 반환
        /// </summary>
        private static Line FindLongestLine(List<Line> lines)
        {
            if (lines == null || lines.Count == 0)
                throw new ArgumentException("Line 그룹이 비어있습니다.");

            Line longestLine = lines[0];
            double maxLength = longestLine.Length;

            foreach (var line in lines.Skip(1))
            {
                if (line.Length > maxLength)
                {
                    maxLength = line.Length;
                    longestLine = line;
                }
            }

            return longestLine;
        }

        /// <summary>
        /// colinear line 처리 결과를 테스트하는 커맨드
        /// </summary>
        [CommandMethod("TEST_COLINEAR")]
        public void TestColinearLineProcessor()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Line 선택
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 line이 없습니다.");
                    return;
                }

                using var tr = db.TransactionManager.StartTransaction();

                // ObjectId에서 Line 객체로 변환
                var lines = selectedLineIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(line => line != null)
                    .ToList();

                ed.WriteMessage($"\n원본 line 개수: {lines.Count}");

                // colinear line 처리
                var processedLines = RemoveRedundantColinearLines(lines);

                ed.WriteMessage($"\n처리 후 line 개수: {processedLines.Count}");
                ed.WriteMessage($"\n제거된 line 개수: {lines.Count - processedLines.Count}");

                // 결과 출력
                for (int i = 0; i < processedLines.Count; i++)
                {
                    var line = processedLines[i];
                    double angle = CalculateLineAngle(line);
                    ed.WriteMessage($"\n남은 Line {i + 1}: 길이 {line.Length:F3}, 각도 {angle:F1}도");
                }

                tr.Commit();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Line 선택 메서드 (첨부 샘플 코드에서 참조)
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
    }


    public class ParallelLineChecker
    {
        // .NET 8.0 기능: 컴파일 타임 상수
        private const double TOLERANCE = 1e-6; // 허용 오차 (점이 선분 내부에 있는지 판단용)
        private const string COMMAND_NAME = "CHKLINEPROJ";

        /// <summary>
        /// 2개의 평행한 line에서 line1의 2점 중 최소한 한개가 수직방향 Projection했을때 line2 내부에 점이 있는지 확인
        /// </summary>
        /// <param name="line1">첫 번째 선분 (투영할 점들이 있는 선분)</param>
        /// <param name="line2">두 번째 선분 (투영 대상 선분)</param>
        /// <returns>line1의 점 중 하나라도 line2 내부에 투영되면 true, 아니면 false</returns>
        public static bool HasVerticalProjectionInside(Line line1, Line line2)
        {
            try
            {
                // Step 1: line1의 StartPoint를 line2에 수직 투영하여 내부에 있는지 확인
                bool startPointInside = IsPointProjectedInside(line1.StartPoint, line2);

                // Step 2: line1의 EndPoint를 line2에 수직 투영하여 내부에 있는지 확인
                bool endPointInside = IsPointProjectedInside(line1.EndPoint, line2);

                // Step 3: 둘 중 하나라도 내부에 있으면 true 반환
                return startPointInside || endPointInside;
            }
            catch (System.Exception)
            {
                return false; // 오류 발생시 false 반환
            }
        }

        /// <summary>
        /// 특정 점이 선분에 수직 투영했을 때 선분 내부에 있는지 확인
        /// </summary>
        /// <param name="point">투영할 점</param>
        /// <param name="targetLine">투영 대상 선분</param>
        /// <returns>투영점이 선분 내부에 있으면 true, 아니면 false</returns>
        public static bool IsPointProjectedInside(Point3d point, Line targetLine)
        {
            try
            {
                // GetClosestPointTo(point, false) 사용 - 연장선 제외, 선분 내부만
                Point3d projectedPoint = targetLine.GetClosestPointTo(point, false);

                // GetClosestPointTo(point, true) 사용 - 연장선 포함
                Point3d projectedPointExtended = targetLine.GetClosestPointTo(point, true);

                // 두 투영점이 거의 같으면 원점이 선분 내부에 투영됨
                double distance = projectedPoint.DistanceTo(projectedPointExtended);
                return distance <= TOLERANCE;
            }
            catch (System.Exception)
            {
                return false; // 오류 발생시 false 반환
            }
        }

        /// <summary>
        /// 투영 정보를 포함한 상세 결과를 반환하는 확장 함수
        /// </summary>
        /// <param name="line1">첫 번째 선분</param>
        /// <param name="line2">두 번째 선분</param>
        /// <returns>투영 결과 정보</returns>
        public static ProjectionResult CheckProjectionWithDetails(Line line1, Line line2)
        {
            var result = new ProjectionResult();

            try
            {
                // line1.StartPoint 투영 확인
                result.StartPointProjection = GetProjectionDetails(line1.StartPoint, line2);

                // line1.EndPoint 투영 확인
                result.EndPointProjection = GetProjectionDetails(line1.EndPoint, line2);

                // 결과 요약
                result.HasAnyInternalProjection = result.StartPointProjection.IsInside ||
                                                result.EndPointProjection.IsInside;

                return result;
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// 한 점의 투영 세부 정보를 구하는 헬퍼 함수
        /// </summary>
        /// <param name="point">투영할 점</param>
        /// <param name="targetLine">투영 대상 선분</param>
        /// <returns>투영 세부 정보</returns>
        private static ProjectionDetail GetProjectionDetails(Point3d point, Line targetLine)
        {
            var detail = new ProjectionDetail();

            try
            {
                detail.OriginalPoint = point;
                detail.ProjectedPoint = targetLine.GetClosestPointTo(point, false); // 선분 내부만
                detail.ProjectedPointExtended = targetLine.GetClosestPointTo(point, true); // 연장선 포함

                // 투영 거리 계산
                detail.ProjectionDistance = point.DistanceTo(detail.ProjectedPoint);

                // 내부 투영 여부 확인
                double internalExternalDiff = detail.ProjectedPoint.DistanceTo(detail.ProjectedPointExtended);
                detail.IsInside = internalExternalDiff <= TOLERANCE;

                return detail;
            }
            catch (System.Exception ex)
            {
                detail.ErrorMessage = ex.Message;
                return detail;
            }
        }

        /// <summary>
        /// AutoCAD 커맨드로 함수 테스트
        /// </summary>
        [CommandMethod(COMMAND_NAME)]
        public void TestParallelLineProjection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: 첫 번째 Line 선택
                Line line1 = SelectLine(ed, "\n첫 번째 선분을 선택하세요: ");
                if (line1 == null) return;

                // Step 2: 두 번째 Line 선택
                Line line2 = SelectLine(ed, "\n두 번째 선분을 선택하세요: ");
                if (line2 == null) return;

                // Step 3: 투영 확인 수행
                var result = CheckProjectionWithDetails(line1, line2);

                // Step 4: 결과 출력
                ed.WriteMessage($"\n=== 투영 결과 ===");
                ed.WriteMessage($"\n전체 결과: {(result.HasAnyInternalProjection ? "내부 투영 있음" : "내부 투영 없음")}");

                ed.WriteMessage($"\n\n--- Line1 StartPoint 투영 ---");
                ed.WriteMessage($"\n원본 점: {result.StartPointProjection.OriginalPoint}");
                ed.WriteMessage($"\n투영점(내부): {result.StartPointProjection.ProjectedPoint}");
                ed.WriteMessage($"\n투영점(연장): {result.StartPointProjection.ProjectedPointExtended}");
                ed.WriteMessage($"\n투영 거리: {result.StartPointProjection.ProjectionDistance:F3}");
                ed.WriteMessage($"\n내부 투영: {(result.StartPointProjection.IsInside ? "예" : "아니오")}");

                ed.WriteMessage($"\n\n--- Line1 EndPoint 투영 ---");
                ed.WriteMessage($"\n원본 점: {result.EndPointProjection.OriginalPoint}");
                ed.WriteMessage($"\n투영점(내부): {result.EndPointProjection.ProjectedPoint}");
                ed.WriteMessage($"\n투영점(연장): {result.EndPointProjection.ProjectedPointExtended}");
                ed.WriteMessage($"\n투영 거리: {result.EndPointProjection.ProjectionDistance:F3}");
                ed.WriteMessage($"\n내부 투영: {(result.EndPointProjection.IsInside ? "예" : "아니오")}");

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    ed.WriteMessage($"\n오류: {result.ErrorMessage}");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Line 선택 헬퍼 함수
        /// </summary>
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
    }

    /// <summary>
    /// 투영 결과를 담는 클래스
    /// </summary>
    public class ProjectionResult
    {
        public ProjectionDetail StartPointProjection { get; set; } = new ProjectionDetail();
        public ProjectionDetail EndPointProjection { get; set; } = new ProjectionDetail();
        public bool HasAnyInternalProjection { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 개별 점의 투영 세부 정보를 담는 클래스
    /// </summary>
    public class ProjectionDetail
    {
        public Point3d OriginalPoint { get; set; }
        public Point3d ProjectedPoint { get; set; }
        public Point3d ProjectedPointExtended { get; set; }
        public double ProjectionDistance { get; set; }
        public bool IsInside { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
    }


    // Find Near Lines
    public class PointLineHighlighter
    {
        private const double BOX_SIZE = 4000.0; // 사각형 박스 크기
        private const short RED_COLOR_INDEX = 1; // AutoCAD Red 색상 인덱스

        /// <summary>
        /// 특정 Point가 특정 Line의 확장되지 않은 실제 선분 위에 수직 투영될 수 있는지 확인하는 함수
        /// (이전에 작성한 함수를 여기서 재사용)
        /// </summary>
        public static bool IsPointProjectableOnLine(Point3d targetPoint, Line line, double tolerance = 1e-6)
        {
            try
            {
                if (line == null)
                    return false;

                // 선분 자체에서만 최근점을 구함 (extend = false)
                Point3d closestOnSegment = line.GetClosestPointTo(targetPoint, false);

                // 연장선을 포함하여 최근점을 구함 (extend = true)
                Point3d closestOnExtended = line.GetClosestPointTo(targetPoint, true);

                // 두 점이 같으면 투영점이 선분 자체에 있음
                double distance = closestOnSegment.DistanceTo(closestOnExtended);
                bool isOnSegment = distance <= tolerance;

                return isOnSegment;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 메인 커맨드: 점 선택 후 주변 Line들 중 투영 가능한 것들을 빨간색으로 변경
        /// </summary>
        [CommandMethod("HighlightProjectableLines")]
        public void Cmd_HighlightProjectableLines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 기준점 선택
                PromptPointOptions pointOpts = new PromptPointOptions("\n기준점을 선택하세요: ");
                pointOpts.AllowNone = false;

                PromptPointResult pointResult = ed.GetPoint(pointOpts);
                if (pointResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n작업이 취소되었습니다.");
                    return;
                }

                Point3d centerPoint = pointResult.Value;
                ed.WriteMessage($"\n기준점: ({centerPoint.X:F2}, {centerPoint.Y:F2})");

                // 2단계: 기준점을 중심으로 하는 2000x2000 사각형 영역 생성
                Point3dCollection selectionPolygon = CreateSelectionBox(centerPoint, BOX_SIZE);

                // 3단계: 사각형 영역에 걸치는 Line 객체들 선택
                List<ObjectId> selectedLineIds = SelectLinesInArea(ed, selectionPolygon);

                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n지정된 영역에서 선분을 찾을 수 없습니다.");
                    return;
                }

                ed.WriteMessage($"\n총 {selectedLineIds.Count}개의 선분을 발견했습니다.");

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    List<Line> lls = new List<Line>();
                    foreach (ObjectId lineId in selectedLineIds)
                    {
                        try
                        {
                            Line line = tr.GetObject(lineId, OpenMode.ForRead) as Line;
                            if (line != null)
                            {
                                lls.Add(line);
                            }
                        }
                        catch (System.Exception)
                        {
                            // 개별 선분 처리 실패 시 계속 진행
                            continue;
                        }
                    }

                   

                    // point 기준으로 가장 가까운 으로 Sorting
                    lls = lls.OrderBy(line => line.GetClosestPointTo(centerPoint, false).DistanceTo(centerPoint)).ToList();
                    // Line 객체들로 그룹화 수행
                    var lineGroups = LineGrouping.GroupLinesByAngleAndDistance(lls, 1.0);

                    // 기준 Point 와 수직 겹침이 있는 lines
                    var lls1 =   lls.Where(x=> x.IsPointProjectableOnLine(centerPoint) == true).ToList();

                    // 각 그룹에서 가장 처음 나오는 Line만 남기기
                    List<Line> finalLines = new List<Line>();   
                    foreach (var group in lineGroups)
                    {
                        finalLines.Add(group[0]);
                    }

                    // 빨간색으로 변경

                    Color redColor = Color.FromColorIndex(ColorMethod.ByAci, RED_COLOR_INDEX);

                    foreach (var ll in finalLines)
                    {
                        try
                        {
                            // 쓰기 모드로 업그레이드하여 색상 변경
                            ll.UpgradeOpen();
                            ll.Color = redColor;
                            ll.DowngradeOpen();

                        }
                        catch (System.Exception)
                        {
                            // 개별 선분 처리 실패 시 계속 진행
                            continue;
                        }
                    }

                    tr.Commit();
                }

                //// 4단계: 선택된 Line들 중 투영 가능한 것들 찾기 및 색상 변경
                //int highlightedCount = HighlightProjectableLines(db, selectedLineIds, centerPoint);

                // 5단계: 결과 출력
                ed.WriteMessage($"\n작업 완료!");
                ed.WriteMessage($"\n - 검사된 선분: {selectedLineIds.Count}개");
                //ed.WriteMessage($"\n - 투영 가능한 선분: {highlightedCount}개");
                //ed.WriteMessage($"\n - 빨간색으로 변경된 선분: {highlightedCount}개");

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 기준점을 중심으로 하는 정사각형 선택 영역 생성
        /// </summary>
        private Point3dCollection CreateSelectionBox(Point3d centerPoint, double boxSize)
        {
            double halfSize = boxSize / 2.0;

            Point3dCollection polygon = new Point3dCollection();

            // 사각형의 네 모서리 점들 (시계방향)
            polygon.Add(new Point3d(centerPoint.X - halfSize, centerPoint.Y - halfSize, centerPoint.Z)); // 좌하
            polygon.Add(new Point3d(centerPoint.X + halfSize, centerPoint.Y - halfSize, centerPoint.Z)); // 우하
            polygon.Add(new Point3d(centerPoint.X + halfSize, centerPoint.Y + halfSize, centerPoint.Z)); // 우상
            polygon.Add(new Point3d(centerPoint.X - halfSize, centerPoint.Y + halfSize, centerPoint.Z)); // 좌상

            return polygon;
        }

        /// <summary>
        /// 지정된 영역에서 Line 객체들을 선택
        /// </summary>
        private List<ObjectId> SelectLinesInArea(Editor ed, Point3dCollection selectionPolygon)
        {
            List<ObjectId> lineIds = new List<ObjectId>();

            try
            {
                // Line 객체만 선택하도록 필터 설정
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "LINE")
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                // Crossing Polygon 선택 수행
                PromptSelectionResult selectionResult = ed.SelectCrossingPolygon(selectionPolygon, filter);

                if (selectionResult.Status == PromptStatus.OK && selectionResult.Value != null)
                {
                    ObjectId[] selectedIds = selectionResult.Value.GetObjectIds();
                    lineIds.AddRange(selectedIds);
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n선분 선택 중 오류: {ex.Message}");
            }

            return lineIds;
        }

        /// <summary>
        /// 선택된 Line들 중 투영 가능한 것들을 찾아서 빨간색으로 변경
        /// </summary>
        private int HighlightProjectableLines(Database db, List<ObjectId> lineIds, Point3d centerPoint)
        {
            int highlightedCount = 1;

            List<Line> lls = new List<Line>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId lineId in lineIds)
                {
                    try
                    {
                        Line line = tr.GetObject(lineId, OpenMode.ForRead) as Line;
                        if (line != null)
                        {
                            lls.Add(line);
                        }
                    }
                    catch (System.Exception)
                    {
                        // 개별 선분 처리 실패 시 계속 진행
                        continue;
                    }
                }

                if(lls.Count < 2)
                {
                    tr.Commit();
                    return 0;
                }

                // point 기준으로 가장 가까운 으로 Sorting
                lls = lls.OrderBy(line => line.GetClosestPointTo(centerPoint, false).DistanceTo(centerPoint)).ToList();
                // Line 객체들로 그룹화 수행
                var lineGroups = LineGrouping.GroupLinesByAngleAndDistance(lls, 1.0);


                // 각 그룹에서 가장 처음 나오는 Line만 남기기 
                List<Line> finalLines = new List<Line>();

                foreach (var group in lineGroups)
                {
                        finalLines.Add(group[0]);
                }

                try
                {
                    Color redColor = Color.FromColorIndex(ColorMethod.ByAci, RED_COLOR_INDEX);

                    foreach (var ll in finalLines)
                    {
                        try
                        {
                                // 쓰기 모드로 업그레이드하여 색상 변경
                                ll.UpgradeOpen();
                                ll.Color = redColor;
                                ll.DowngradeOpen();

                                highlightedCount++;
                  
                        }
                        catch (System.Exception)
                        {
                            // 개별 선분 처리 실패 시 계속 진행
                            continue;
                        }
                    }

                    tr.Commit();
                }
                catch (System.Exception)
                {
                    tr.Abort();
                    throw;
                }
            }

            return highlightedCount;
        }

        /// <summary>
        /// 디버그용 커맨드: 선택 영역을 시각적으로 표시
        /// </summary>
        [CommandMethod("ShowSelectionBox")]
        public void ShowSelectionBoxCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 기준점 선택
                PromptPointOptions pointOpts = new PromptPointOptions("\n기준점을 선택하세요: ");
                PromptPointResult pointResult = ed.GetPoint(pointOpts);

                if (pointResult.Status != PromptStatus.OK)
                    return;

                Point3d centerPoint = pointResult.Value;

                // 선택 영역 생성
                Point3dCollection selectionBox = CreateSelectionBox(centerPoint, BOX_SIZE);

                // 선택 영역을 선으로 그리기
                DrawSelectionBox(db, selectionBox);

                ed.WriteMessage($"\n기준점 ({centerPoint.X:F2}, {centerPoint.Y:F2})을 중심으로 하는");
                ed.WriteMessage($"\n{BOX_SIZE}x{BOX_SIZE} 선택 영역이 표시되었습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택 영역을 시각적으로 표시하는 보조 함수
        /// </summary>
        private void DrawSelectionBox(Database db, Point3dCollection boxPoints)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // 사각형의 네 변을 그리기
                    for (int i = 0; i < boxPoints.Count; i++)
                    {
                        Point3d startPoint = boxPoints[i];
                        Point3d endPoint = boxPoints[(i + 1) % boxPoints.Count]; // 마지막 점은 첫 번째 점과 연결

                        Line boxLine = new Line(startPoint, endPoint);
                        boxLine.Color = Color.FromColorIndex(ColorMethod.ByAci, 3); // 녹색으로 표시

                        btr.AppendEntity(boxLine);
                        tr.AddNewlyCreatedDBObject(boxLine, true);
                    }

                    tr.Commit();
                }
                catch (System.Exception)
                {
                    tr.Abort();
                    throw;
                }
            }
        }

        /// <summary>
        /// 색상 복원 커맨드: 빨간색 선분들을 원래 색상(ByLayer)으로 복원
        /// </summary>
        [CommandMethod("RestoreLineColors")]
        public void RestoreLineColorsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 빨간색 Line 객체들을 선택
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "LINE"),
                    new TypedValue((int)DxfCode.Color, RED_COLOR_INDEX)
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                PromptSelectionResult selectionResult = ed.SelectAll(filter);

                if (selectionResult.Status != PromptStatus.OK || selectionResult.Value == null)
                {
                    ed.WriteMessage("\n복원할 빨간색 선분을 찾을 수 없습니다.");
                    return;
                }

                ObjectId[] redLineIds = selectionResult.Value.GetObjectIds();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    int restoredCount = 0;
                    Color byLayerColor = Color.FromColorIndex(ColorMethod.ByLayer, 256);

                    foreach (ObjectId lineId in redLineIds)
                    {
                        try
                        {
                            Line line = tr.GetObject(lineId, OpenMode.ForWrite) as Line;
                            if (line != null)
                            {
                                line.Color = byLayerColor;
                                restoredCount++;
                            }
                        }
                        catch (System.Exception)
                        {
                            continue;
                        }
                    }

                    tr.Commit();
                    ed.WriteMessage($"\n{restoredCount}개의 선분 색상이 복원되었습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }

}
