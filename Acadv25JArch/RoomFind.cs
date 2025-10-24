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

namespace AutoCADLineIntersection
{
    public class LineIntersectionFinder
    {
        // .NET 8.0 기능: 컴파일 타임 상수
        private const string COMMAND_NAME = "FINDINTERSECTION";
        private const short RED_COLOR_INDEX = 1; // AutoCAD ACI Red 색상
        private const double MIN_SEARCH_DISTANCE = 100.0; // 최소 검색 거리
        private const double SEARCH_EXPANSION_RATIO = 0.5; // 라인 길이 대비 검색 확장 비율

        /// <summary>
        /// 선택된 line과 교차하는 모든 line을 찾아서 색상을 red로 변경하는 메인 커맨드
        /// </summary>
        [CommandMethod("Find_intersect")]
        public void Cmd_FindIntersectingLines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {


                // 2단계: 기준 라인 근처의 Line 객체들만 가져오기 (성능 최적화)
                using var tr = db.TransactionManager.StartTransaction();

                // 1단계: 기준 Line 선택
                Line selectedLine = SelectSingleLine(ed, "\n기준이 될 라인을 선택하세요: ",tr);
                if (selectedLine == null)
                {
                    ed.WriteMessage("\n라인이 선택되지 않았습니다.");
                    return;
                }


                var nearbyLines = GetNearbyLinesUsingPolygon(selectedLine);//, ed);// tr);
                if (nearbyLines.Count == 0)
                {
                    ed.WriteMessage("\n기준 라인 근처에서 다른 라인을 찾을 수 없습니다.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage($"\n기준 라인 근처에서 {nearbyLines.Count}개의 라인을 찾았습니다.");

                // 3단계: 교차하는 Line들 찾기
                var intersectingLines = FindIntersectingLinesWithBase(selectedLine, nearbyLines);

                // 기준점에서 거리로 정열

                intersectingLines = intersectingLines.OrderBy(x=> x.intersectionPoints[0].DistanceTo(selectedLine.StartPoint)).ToList();

                intersectingLines = intersectingLines.Take(1).ToList(); 

                // 4단계: 교차하는 Line들의 색상을 Red로 변경
                int changedCount = ChangeLinesToRed(intersectingLines, tr);

                tr.Commit();

                // 5단계: 결과 출력
                ed.WriteMessage($"\n기준 라인과 교차하는 {changedCount}개의 라인을 빨간색으로 변경했습니다.");

                if (intersectingLines.Count > 0)
                {
                    ed.WriteMessage("\n교차 정보:");
                    for (int i = 0; i < intersectingLines.Count; i++)
                    {
                        var intersectionInfo = intersectingLines[i];
                        ed.WriteMessage($"\n  라인 {i + 1}: {intersectionInfo.intersectionPoints.Count}개 교차점");

                        for (int j = 0; j < intersectionInfo.intersectionPoints.Count; j++)
                        {
                            var point = intersectionInfo.intersectionPoints[j];
                            ed.WriteMessage($"\n    교차점 {j + 1}: ({point.X:F3}, {point.Y:F3}, {point.Z:F3})");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// 선택된 line과 교차하는 모든 line을 찾아서 색상을 red로 변경하는 메인 커맨드
        /// </summary>
        [CommandMethod("Find_intersect_Multi")]
        public void Cmd_FindIntersectingLines1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 2단계: 기준 라인 근처의 Line 객체들만 가져오기 (성능 최적화)
                using var tr = db.TransactionManager.StartTransaction();

                // 1단계: 기준 Line 선택
                Line selectedLine = SelectSingleLine(ed, "\n기준이 될 라인을 선택하세요: ", tr);
                if (selectedLine == null)
                {
                    ed.WriteMessage("\n라인이 선택되지 않았습니다.");
                    return;
                }

                // 3. 현재 공간의 BlockTableRecord 가져오기
                BlockTableRecord currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                selectedLine.UpgradeOpen();


                // 5. 30도씩 12번 회전 (360도 = 12 × 30도)
                for (int i = 1; i <= 12; i++)
                {

                    // 각도를 라디안으로 변환 (30도 = π/6)
                    double angle = Math.PI / 6; // 30도

                    // StartPoint 중심 30도 회전 행렬 생성
                    Matrix3d rotationMatrix = Matrix3d.Rotation(angle, Vector3d.ZAxis, selectedLine.StartPoint);


                    // 새 Line 생성 (원본 복사)
                    //Line newLine = selectedLine.Clone() as Line;

                    // 회전 적용
                    selectedLine.TransformBy(rotationMatrix);

                    ////// 도면에 추가
                    //currentSpace.AppendEntity(newLine);
                    //tr.AddNewlyCreatedDBObject(newLine, true);

                    var nearbyLines = GetNearbyLinesUsingPolygon(selectedLine);//, ed);// tr);
                    if (nearbyLines.Count == 0) continue;
                    //{
                    //    ed.WriteMessage("\n기준 라인 근처에서 다른 라인을 찾을 수 없습니다.");
                    //    tr.Commit();
                    //    return;
                    //}

                    ed.WriteMessage($"\n기준 라인 근처에서 {nearbyLines.Count}개의 라인을 찾았습니다.");

                    // 3단계: 교차하는 Line들 찾기
                    var intersectingLines = FindIntersectingLinesWithBase(selectedLine, nearbyLines);
                    if (intersectingLines.Count == 0) continue;

                    intersectingLines = intersectingLines.OrderBy(x => x.intersectionPoints[0].DistanceTo(selectedLine.StartPoint)).ToList();

                    intersectingLines = intersectingLines.Take(1).ToList();

                    // 4단계: 교차하는 Line들의 색상을 Red로 변경
                    int changedCount = ChangeLinesToRed(intersectingLines, tr);

                    nearbyLines.Clear();
                    intersectingLines.Clear();  


                    // 진행 상황 출력
                    ed.WriteMessage($"\n{i * 30}도 회전된 Line 생성됨");
                }



                // 기준점에서 거리로 정열



                tr.Commit();

                //// 5단계: 결과 출력
                //ed.WriteMessage($"\n기준 라인과 교차하는 {changedCount}개의 라인을 빨간색으로 변경했습니다.");

                //if (intersectingLines.Count > 0)
                //{
                //    ed.WriteMessage("\n교차 정보:");
                //    for (int i = 0; i < intersectingLines.Count; i++)
                //    {
                //        var intersectionInfo = intersectingLines[i];
                //        ed.WriteMessage($"\n  라인 {i + 1}: {intersectionInfo.intersectionPoints.Count}개 교차점");

                //        for (int j = 0; j < intersectionInfo.intersectionPoints.Count; j++)
                //        {
                //            var point = intersectionInfo.intersectionPoints[j];
                //            ed.WriteMessage($"\n    교차점 {j + 1}: ({point.X:F3}, {point.Y:F3}, {point.Z:F3})");
                //        }
                //    }
                //}
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        /// <summary>
        /// 단일 Line 선택 메서드 (Line 객체 직접 반환)
        /// </summary>
        private Line SelectSingleLine(Editor ed, string prompt,Transaction tr)
        {
            PromptEntityOptions peo = new PromptEntityOptions(prompt);
            peo.SetRejectMessage("\nLine 객체만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
                return null;

            Line line = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
            return line;
        }

        /// <summary>
        /// 기준 라인 근처의 Line 객체들만 가져오기 (SelectCrossingPolygon 사용 - 성능 최적화)
        /// </summary>
        private List<(Line line, ObjectId id)> GetNearbyLinesUsingPolygon(Line baseLine)//, Editor ed)//, Transaction tr)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            var nearbyLines = new List<(Line line, ObjectId id)>();   
            try
            {
                // 1단계: 기준 라인의 경계 박스를 구하고 확장
                var searchPolygon = baseLine.GetEntPoly().GetPointCollections(true);//          CreateSearchPolygonAroundLine(baseLine); //baseLine.GetPoly();

                // 2단계: Line만 선택하도록 필터 설정
                TypedValue[] filterList = [
                    new TypedValue((int)DxfCode.Start, "LINE")
                ];
                var filter = new SelectionFilter(filterList);

                // 3단계: SelectCrossingPolygon을 사용하여 근처 라인들 선택
                var selectionResult = ed.SelectCrossingPolygon(searchPolygon, filter);

                if (selectionResult.Status == PromptStatus.OK && selectionResult.Value != null)
                {
                    var selectedIds = selectionResult.Value.GetObjectIds();

                    // 4단계: 선택된 ObjectId들을 Line 객체로 변환
                    foreach (ObjectId objId in selectedIds)
                    {
                        if (objId == baseLine.Id) continue;
                        if (objId.GetEntity() is Line line)
                        {
                            nearbyLines.Add((line, objId));
                        }


                        //if (tr.GetObject(objId, OpenMode.ForRead) is Line line)
                        //{
                        //    nearbyLines.Add((line, objId));
                        //}
                    }
                }
                
            }
            catch (System.Exception)
            {
                // SelectCrossingPolygon 실패 시 빈 리스트 반환
                // 실제 환경에서는 로그를 남기거나 대체 방법을 사용할 수 있음
            }

            

            return nearbyLines;
        }

        /// <summary>
        /// 기준 라인 주변의 검색 폴리곤을 생성하는 메서드
        /// </summary>
        private Point3dCollection CreateSearchPolygonAroundLine(Line baseLine)
        {
            // 확장 거리 (라인 길이의 50% + 최소 100 단위)
            double expansionDistance = System.Math.Max(baseLine.Length * SEARCH_EXPANSION_RATIO, MIN_SEARCH_DISTANCE);

            // 라인의 시작점과 끝점
            Point3d startPt = baseLine.StartPoint;
            Point3d endPt = baseLine.EndPoint;

            // 라인의 방향 벡터와 수직 벡터 계산
            Vector3d lineDirection = (endPt - startPt).GetNormal();
            Vector3d perpendicular = new Vector3d(-lineDirection.Y, lineDirection.X, 0).GetNormal();

            // 확장된 점들 계산
            Vector3d expansionVector = perpendicular * 10.0;// * expansionDistance;
            //Vector3d lengthExpansion = lineDirection * expansionDistance;

            // 직사각형 폴리곤의 4개 꼭짓점 생성
            Point3d corner1 = startPt  + expansionVector;  // 좌상단
            Point3d corner2 = endPt + expansionVector;    // 우상단  
            Point3d corner3 = endPt - expansionVector;    // 우하단
            Point3d corner4 = startPt - expansionVector;  // 좌하단

            // Point3dCollection에 폴리곤 점들 추가 (시계방향)
            var polygon = new Point3dCollection
            {
                corner1,
                corner2,
                corner3,
                corner4
            };

            return polygon;
        }

        /// <summary>
        /// 기준 Line과 교차하는 Line들을 찾는 메서드 (AutoCAD 2025 API IntersectWith 사용)
        /// </summary>
        private List<(Line line, ObjectId id, Point3dCollection intersectionPoints)> FindIntersectingLinesWithBase(
            Line baseLine,
            List<(Line line, ObjectId id)> allLines)
        {
            var intersectingLines = new List<(Line line, ObjectId id, Point3dCollection intersectionPoints)>();

            foreach (var (line, id) in allLines)
            {
                try
                {
                    // 같은 라인인지 확인 (Handle로 비교)
                    if (line.Handle == baseLine.Handle)
                        continue;

                    // 교차점 확인을 위한 Point3dCollection
                    var intersectionPoints = new Point3dCollection();

                    // AutoCAD 2025 API IntersectWith 메서드 사용
                    // OnBothOperands: 실제 교차하는 점만 계산 (연장하지 않음)
                    baseLine.IntersectWith(
                        line,
                        Intersect.OnBothOperands,
                        intersectionPoints,
                        System.IntPtr.Zero,
                        System.IntPtr.Zero);

                    // 교차점이 있으면 리스트에 추가
                    if (intersectionPoints.Count > 0)
                    {
                        intersectingLines.Add((line, id, intersectionPoints));
                    }
                }
                catch (System.Exception)
                {
                    // 개별 교차 확인 실패 시 무시하고 계속 진행
                    continue;
                }
            }

            return intersectingLines;
        }

        /// <summary>
        /// 교차하는 Line들의 색상을 Red로 변경하는 메서드
        /// </summary>
        private int ChangeLinesToRed(
            List<(Line line, ObjectId id, Point3dCollection intersectionPoints)> intersectingLines,
            Transaction tr)
        {
            int changedCount = 0;
            var redColor = Color.FromColorIndex(ColorMethod.ByAci, RED_COLOR_INDEX);

            foreach (var (line, id, _) in intersectingLines)
            {
                try
                {
                    // ObjectId로 실제 Entity를 ForWrite 모드로 열기
                    if (tr.GetObject(id, OpenMode.ForWrite) is Line writableLine)
                    {
                        writableLine.Color = redColor;
                        changedCount++;
                    }
                }
                catch (System.Exception)
                {
                    // 개별 색상 변경 실패 시 무시하고 계속 진행
                    continue;
                }
            }

            return changedCount;
        }

        /// <summary>
        /// 두 Line이 교차하는지 확인하는 유틸리티 메서드 (단순 확인용)
        /// </summary>
        private static bool DoLinesIntersect(Line line1, Line line2)
        {
            try
            {
                var intersectionPoints = new Point3dCollection();

                line1.IntersectWith(
                    line2,
                    Intersect.OnBothOperands,
                    intersectionPoints,
                    System.IntPtr.Zero,
                    System.IntPtr.Zero);

                return intersectionPoints.Count > 0;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 교차 통계 정보를 출력하는 추가 커맨드
        /// </summary>
        [CommandMethod("INTERSECTION_STATS")]
        public void Cmd_ShowIntersectionStatistics()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using var tr = db.TransactionManager.StartTransaction();
                Line selectedLine = SelectSingleLine(ed, "\n통계를 확인할 기준 라인을 선택하세요: ", tr);
                if (selectedLine == null)
                {
                    ed.WriteMessage("\n라인이 선택되지 않았습니다.");
                    return;
                }

                // 근처 라인들만 가져와서 성능 최적화
                var nearbyLines = GetNearbyLinesUsingPolygon(selectedLine);//, ed);//, tr);
                var intersectingLines = FindIntersectingLinesWithBase(selectedLine, nearbyLines);

                tr.Commit();

                // 통계 정보 출력
                ed.WriteMessage($"\n=== 교차 통계 정보 ===");
                ed.WriteMessage($"\n기준 라인 근처의 라인 수: {nearbyLines.Count}개");
                ed.WriteMessage($"\n기준 라인과 교차하는 라인 수: {intersectingLines.Count}개");

                if (nearbyLines.Count > 0)
                {
                    ed.WriteMessage($"\n교차 비율: {(intersectingLines.Count * 100.0 / nearbyLines.Count):F1}%");
                }

                if (intersectingLines.Count > 0)
                {
                    // 교차점 수별 분류
                    var pointCountGroups = intersectingLines
                        .GroupBy(x => x.intersectionPoints.Count)
                        .OrderBy(g => g.Key);

                    ed.WriteMessage($"\n\n교차점 수별 분류:");
                    foreach (var group in pointCountGroups)
                    {
                        ed.WriteMessage($"\n  {group.Key}개 교차점: {group.Count()}개 라인");
                    }

                    // 기준 라인 정보
                    ed.WriteMessage($"\n\n기준 라인 정보:");
                    ed.WriteMessage($"\n  시작점: ({selectedLine.StartPoint.X:F3}, {selectedLine.StartPoint.Y:F3}, {selectedLine.StartPoint.Z:F3})");
                    ed.WriteMessage($"\n  끝점: ({selectedLine.EndPoint.X:F3}, {selectedLine.EndPoint.Y:F3}, {selectedLine.EndPoint.Z:F3})");
                    ed.WriteMessage($"\n  길이: {selectedLine.Length:F3}");
                    ed.WriteMessage($"\n  각도: {selectedLine.Angle * 180.0 / System.Math.PI:F1}도");

                    // 검색 영역 정보
                    double expansionDistance = System.Math.Max(selectedLine.Length * SEARCH_EXPANSION_RATIO, MIN_SEARCH_DISTANCE);
                    ed.WriteMessage($"\n\n검색 영역 정보:");
                    ed.WriteMessage($"\n  확장 거리: {expansionDistance:F1}");
                    ed.WriteMessage($"\n  검색 영역: 기준 라인 중심 직사각형");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 검색 영역을 시각적으로 표시하는 디버그 커맨드
        /// </summary>
        [CommandMethod("SHOW_SEARCH_AREA")]
        public void Cmd_ShowSearchArea()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using var tr = db.TransactionManager.StartTransaction();

                Line selectedLine = SelectSingleLine(ed, "\n검색 영역을 표시할 기준 라인을 선택하세요: ", tr);
                if (selectedLine == null)
                {
                    ed.WriteMessage("\n라인이 선택되지 않았습니다.");
                    return;
                }

                // 검색 폴리곤 생성
                var searchPolygon = CreateSearchPolygonAroundLine(selectedLine);

                // 폴리곤을 Polyline으로 변환하여 도면에 표시
                var polyline = new Polyline();

                for (int i = 0; i < searchPolygon.Count; i++)
                {
                    var point = searchPolygon[i];
                    polyline.AddVertexAt(i, new Point2d(point.X, point.Y), 0, 0, 0);
                }

                // 폴리곤 닫기
                polyline.Closed = true;

                // 검색 영역을 노란색으로 표시
                polyline.Color = Color.FromColorIndex(ColorMethod.ByAci, 2); // Yellow

                // 도면에 추가
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord modelSpace = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                modelSpace.AppendEntity(polyline);
                tr.AddNewlyCreatedDBObject(polyline, true);

                tr.Commit();

                double expansionDistance = System.Math.Max(selectedLine.Length * SEARCH_EXPANSION_RATIO, MIN_SEARCH_DISTANCE);
                ed.WriteMessage($"\n검색 영역이 노란색 폴리곤으로 표시되었습니다.");
                ed.WriteMessage($"\n확장 거리: {expansionDistance:F1}");
                ed.WriteMessage($"\n이 영역 내의 라인들만 교차 확인 대상이 됩니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }


    //

    public class LineBlockIntersection
    {
        // .NET 8.0 기능: 컴파일 타임 상수
        private const string COMMAND_NAME = "CHECKLINEBLOCKINTERSECT";
        private const string DEMO_COMMAND = "DEMOINTERSECT";
        private const string ORIGIN_COMMAND = "CHECKLINEORIGIN";
        private const string GEOMETRY_COMMAND = "CHECKLINEGEOMETRY";
        private const double DEFAULT_TOLERANCE = 50.0; // 기본 교차 판정 거리 (50mm)

        /// <summary>
        /// Line과 Block이 교차하는지 확인 (Block 원점에서 Line까지 최단거리 기준)
        /// </summary>
        /// <param name="line">검사할 Line 객체</param>
        /// <param name="blockRef">검사할 BlockReference 객체</param>
        /// <param name="tolerance">교차 판정 허용 거리 (기본값: 50mm)</param>
        /// <returns>교차 여부</returns>
        public static bool DoesLineIntersectBlock(Line line, BlockReference blockRef, double tolerance = DEFAULT_TOLERANCE)
        {
            if (line == null || blockRef == null)
                return false;

            try
            {
                // Step 1: Block 원점에서 Line까지의 최단거리 계산
                double distance = GetDistanceFromBlockOriginToLine(line, blockRef);

                // Step 2: 허용 거리 내에 있으면 교차로 판정
                return distance <= tolerance;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Block 원점에서 Line까지의 최단거리 계산
        /// </summary>
        /// <param name="line">검사할 Line 객체</param>
        /// <param name="blockRef">검사할 BlockReference 객체</param>
        /// <returns>최단거리</returns>
        public static double GetDistanceFromBlockOriginToLine(Line line, BlockReference blockRef)
        {
            if (line == null || blockRef == null)
                return double.MaxValue;

            try
            {
                // Step 1: Block의 원점(insertion point) 가져오기
                Point3d blockOrigin = blockRef.Position;

                // Step 2: 원점에서 Line으로의 수직 투영점 찾기 (첨부 샘플에서 우선 권장)
                Point3d closestPointOnLine = line.GetClosestPointTo(blockOrigin, true);

                // Step 3: 거리 계산
                return blockOrigin.DistanceTo(closestPointOnLine);
            }
            catch (System.Exception)
            {
                return double.MaxValue;
            }
        }

        /// <summary>
        /// Line과 Block의 교차점 반환 (Block 원점에서 Line으로의 수직 투영점)
        /// </summary>
        /// <param name="line">검사할 Line 객체</param>
        /// <param name="blockRef">검사할 BlockReference 객체</param>
        /// <param name="tolerance">교차 판정 허용 거리</param>
        /// <returns>교차점 (교차하지 않으면 빈 배열)</returns>
        public static Point3d[] GetLineBlockIntersectionPoints(Line line, BlockReference blockRef, double tolerance = DEFAULT_TOLERANCE)
        {
            if (line == null || blockRef == null)
                return [];

            try
            {
                // Step 1: Block 원점에서 Line까지의 거리 확인
                double distance = GetDistanceFromBlockOriginToLine(line, blockRef);

                // Step 2: 허용 거리 내에 있으면 교차점 반환
                if (distance <= tolerance)
                {
                    Point3d blockOrigin = blockRef.Position;
                    Point3d intersectionPoint = line.GetClosestPointTo(blockOrigin, true);
                    return [intersectionPoint];
                }

                return [];
            }
            catch (System.Exception)
            {
                return [];
            }
        }

        /// <summary>
        /// Line과 Block의 교차 정보를 상세하게 반환 (원점 기준)
        /// </summary>
        /// <param name="line">검사할 Line 객체</param>
        /// <param name="blockRef">검사할 BlockReference 객체</param>
        /// <param name="tolerance">교차 판정 허용 거리</param>
        /// <returns>교차 정보 객체</returns>
        public static LineBlockIntersectionInfo GetDetailedIntersectionInfo(Line line, BlockReference blockRef, double tolerance = DEFAULT_TOLERANCE)
        {
            var result = new LineBlockIntersectionInfo
            {
                HasIntersection = false,
                IntersectionPoints = [],
                LineLength = 0,
                BlockName = "",
                BlockOrigin = Point3d.Origin,
                DistanceToBlock = double.MaxValue,
                Tolerance = tolerance
            };

            if (line == null || blockRef == null)
                return result;

            try
            {
                // Step 1: 기본 정보 설정
                result.LineLength = line.Length;
                result.BlockName = blockRef.Name;
                result.BlockOrigin = blockRef.Position;
                result.Tolerance = tolerance;

                // Step 2: Block 원점에서 Line까지의 거리 계산
                result.DistanceToBlock = GetDistanceFromBlockOriginToLine(line, blockRef);

                // Step 3: 교차 판정 및 교차점 계산
                if (result.DistanceToBlock <= tolerance)
                {
                    result.HasIntersection = true;
                    Point3d intersectionPoint = line.GetClosestPointTo(blockRef.Position, true);
                    result.IntersectionPoints = [intersectionPoint];
                }

                return result;
            }
            catch (System.Exception)
            {
                return result;
            }
        }

        /// <summary>
        /// Block의 원점이 Line 위에 정확히 있는지 확인
        /// </summary>
        /// <param name="line">검사할 Line 객체</param>
        /// <param name="blockRef">검사할 BlockReference 객체</param>
        /// <param name="tolerance">허용 오차 (기본값: 1e-6)</param>
        /// <returns>원점이 Line 위에 있는지 여부</returns>
        public static bool IsBlockOriginOnLine(Line line, BlockReference blockRef, double tolerance = 1e-6)
        {
            if (line == null || blockRef == null)
                return false;

            try
            {
                double distance = GetDistanceFromBlockOriginToLine(line, blockRef);
                return distance <= tolerance;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Block 원점에서 Line의 시작점과 끝점까지의 거리 계산
        /// </summary>
        /// <param name="line">검사할 Line 객체</param>
        /// <param name="blockRef">검사할 BlockReference 객체</param>
        /// <returns>시작점 거리, 끝점 거리, 수직 투영점 거리</returns>
        public static (double toStart, double toEnd, double toClosest) GetDistancesToLinePoints(Line line, BlockReference blockRef)
        {
            if (line == null || blockRef == null)
                return (double.MaxValue, double.MaxValue, double.MaxValue);

            try
            {
                Point3d blockOrigin = blockRef.Position;

                double distanceToStart = blockOrigin.DistanceTo(line.StartPoint);
                double distanceToEnd = blockOrigin.DistanceTo(line.EndPoint);
                double distanceToClosest = GetDistanceFromBlockOriginToLine(line, blockRef);

                return (distanceToStart, distanceToEnd, distanceToClosest);
            }
            catch (System.Exception)
            {
                return (double.MaxValue, double.MaxValue, double.MaxValue);
            }
        }

        #region Block 내부 Geometry와의 실제 교차 검사 (기존 방식)

        /// <summary>
        /// Line과 Block 내부 geometry의 실제 교차점 반환 (기존 IntersectWith 방식)
        /// </summary>
        /// <param name="line">검사할 Line 객체</param>
        /// <param name="blockRef">검사할 BlockReference 객체</param>
        /// <returns>실제 교차점들</returns>
        public static Point3d[] GetActualGeometryIntersectionPoints(Line line, BlockReference blockRef)
        {
            if (line == null || blockRef == null)
                return [];

            try
            {
                var intersectionPoints = new Point3dCollection();

                line.IntersectWith(blockRef,
                    Intersect.OnBothOperands,
                    intersectionPoints,
                    IntPtr.Zero,
                    IntPtr.Zero);

                var points = new Point3d[intersectionPoints.Count];
                for (int i = 0; i < intersectionPoints.Count; i++)
                {
                    points[i] = intersectionPoints[i];
                }

                return points;
            }
            catch (System.Exception)
            {
                return [];
            }
        }

        /// <summary>
        /// Line과 Block 내부 geometry가 실제로 교차하는지 확인
        /// </summary>
        /// <param name="line">검사할 Line 객체</param>
        /// <param name="blockRef">검사할 BlockReference 객체</param>
        /// <returns>실제 교차 여부</returns>
        public static bool DoesLineIntersectBlockGeometry(Line line, BlockReference blockRef)
        {
            if (line == null || blockRef == null)
                return false;

            try
            {
                var intersectionPoints = new Point3dCollection();
                line.IntersectWith(blockRef, Intersect.OnBothOperands, intersectionPoints, IntPtr.Zero, IntPtr.Zero);
                return intersectionPoints.Count > 0;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Line과 Block 교차 검사 비교 데모 명령어
        /// </summary>
        [CommandMethod("Line_Block_intersect")]
        public void Cmd_DemoLineBlockIntersection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // Step 1: Line 선택
                Line selectedLine = SelectLine(ed, "\n라인을 선택하세요: ");
                if (selectedLine == null) return;

                // Step 2: Block 선택
                BlockReference selectedBlock = SelectBlock(ed, "\n블록을 선택하세요: ");
                if (selectedBlock == null) return;

                // Step 3: 허용 거리 입력
                var tolPrompt = new PromptDoubleOptions($"\n교차 판정 허용 거리를 입력하세요 (기본값: {DEFAULT_TOLERANCE}): ")
                {
                    DefaultValue = DEFAULT_TOLERANCE,
                    AllowNegative = false,
                    AllowZero = true
                };

                var tolResult = ed.GetDouble(tolPrompt);
                double tolerance = tolResult.Status == PromptStatus.OK ? tolResult.Value : DEFAULT_TOLERANCE;

                ed.WriteMessage($"\n=== Line-Block 교차 검사 비교 ===");
                ed.WriteMessage($"\n블록 이름: {selectedBlock.Name}");
                ed.WriteMessage($"\n블록 원점: X={selectedBlock.Position.X:F3}, Y={selectedBlock.Position.Y:F3}, Z={selectedBlock.Position.Z:F3}");
                ed.WriteMessage($"\n라인 길이: {selectedLine.Length:F3}");
                ed.WriteMessage($"\n허용 거리: {tolerance:F3}");

                // Step 4: 원점 기준 교차 검사 (새로운 방식)
                var intersectionInfo = GetDetailedIntersectionInfo(selectedLine, selectedBlock, tolerance);
                ed.WriteMessage($"\n\n📍 원점 기준 교차 검사 (새로운 방식):");
                ed.WriteMessage($"\n  원점까지 거리: {intersectionInfo.DistanceToBlock:F3}");
                ed.WriteMessage($"\n  교차 여부: {(intersectionInfo.HasIntersection ? "예" : "아니오")}");

                if (intersectionInfo.HasIntersection)
                {
                    var pt = intersectionInfo.IntersectionPoints[0];
                    ed.WriteMessage($"\n  교차점 (수직투영점): X={pt.X:F3}, Y={pt.Y:F3}, Z={pt.Z:F3}");
                }

                // Step 5: 거리 정보
                var distances = GetDistancesToLinePoints(selectedLine, selectedBlock);
                ed.WriteMessage($"\n\n📏 거리 정보:");
                ed.WriteMessage($"\n  원점 → Line 시작점: {distances.toStart:F3}");
                ed.WriteMessage($"\n  원점 → Line 끝점: {distances.toEnd:F3}");
                ed.WriteMessage($"\n  원점 → Line 수직투영점: {distances.toClosest:F3}");

                // Step 6: Block 내부 geometry와의 실제 교차 (기존 방식 비교)
                bool actualIntersect = DoesLineIntersectBlockGeometry(selectedLine, selectedBlock);
                var actualPoints = GetActualGeometryIntersectionPoints(selectedLine, selectedBlock);

                ed.WriteMessage($"\n\n🔧 Block 내부 geometry와 실제 교차 (기존 방식):");
                ed.WriteMessage($"\n  실제 교차 여부: {(actualIntersect ? "예" : "아니오")}");
                ed.WriteMessage($"\n  실제 교차점 개수: {actualPoints.Length}");

                for (int i = 0; i < actualPoints.Length; i++)
                {
                    var pt = actualPoints[i];
                    ed.WriteMessage($"\n    실제 교차점 {i + 1}: X={pt.X:F3}, Y={pt.Y:F3}, Z={pt.Z:F3}");
                }

                // Step 7: 결론
                ed.WriteMessage($"\n\n💡 교차점 정의 방식:");
                ed.WriteMessage($"\n  새로운 방식: Block 원점에서 Line으로의 수직 투영점");
                ed.WriteMessage($"\n  기존 방식: Block 내부 entities와의 실제 교차점");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 원점 기준 교차 검사 명령어
        /// </summary>
        [CommandMethod("Check_line_intersect")]
        public void Cmd_CheckLineBlockIntersection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // Line과 Block 선택
                Line line = SelectLine(ed, "\n라인을 선택하세요: ");
                if (line == null) return;

                BlockReference block = SelectBlock(ed, "\n블록을 선택하세요: ");
                if (block == null) return;

                // 허용 거리 입력
                var tolPrompt = new PromptDoubleOptions($"\n교차 판정 허용 거리 (기본값: {DEFAULT_TOLERANCE}): ")
                {
                    DefaultValue = DEFAULT_TOLERANCE,
                    AllowNegative = false,
                    AllowZero = true
                };

                var tolResult = ed.GetDouble(tolPrompt);
                double tolerance = tolResult.Status == PromptStatus.OK ? tolResult.Value : DEFAULT_TOLERANCE;

                // 교차 검사
                bool intersects = DoesLineIntersectBlock(line, block, tolerance);
                double distance = GetDistanceFromBlockOriginToLine(line, block);

                ed.WriteMessage($"\n=== 원점 기준 교차 검사 결과 ===");
                ed.WriteMessage($"\n원점까지 거리: {distance:F3}");
                ed.WriteMessage($"\n허용 거리: {tolerance:F3}");
                ed.WriteMessage($"\n결과: {(intersects ? "교차함" : "교차하지 않음")}");

                if (intersects)
                {
                    var points = GetLineBlockIntersectionPoints(line, block, tolerance);
                    if (points.Length > 0)
                    {
                        var pt = points[0];
                        ed.WriteMessage($"\n교차점 (수직투영점): X={pt.X:F3}, Y={pt.Y:F3}, Z={pt.Z:F3}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Block 내부 geometry와의 실제 교차 검사 명령어
        /// </summary>
        [CommandMethod("Check_Geo_intersect")]
        public void Cmd_CheckLineGeometryIntersection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                Line line = SelectLine(ed, "\n라인을 선택하세요: ");
                if (line == null) return;

                BlockReference block = SelectBlock(ed, "\n블록을 선택하세요: ");
                if (block == null) return;

                bool intersects = DoesLineIntersectBlockGeometry(line, block);
                var points = GetActualGeometryIntersectionPoints(line, block);

                ed.WriteMessage($"\n=== Block 내부 geometry 교차 검사 ===");
                ed.WriteMessage($"\n교차 여부: {(intersects ? "예" : "아니오")}");
                ed.WriteMessage($"\n교차점 개수: {points.Length}");

                for (int i = 0; i < points.Length; i++)
                {
                    var pt = points[i];
                    ed.WriteMessage($"\n  교차점 {i + 1}: X={pt.X:F3}, Y={pt.Y:F3}, Z={pt.Z:F3}");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Line 객체 선택 헬퍼 메서드
        /// </summary>
        private Line SelectLine(Editor ed, string prompt)
        {
            var peo = new PromptEntityOptions(prompt);
            peo.SetRejectMessage("\nLine 객체만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(Line), true);

            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return null;

            using var tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
            var line = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Line;
            tr.Commit();
            return line;
        }

        /// <summary>
        /// BlockReference 객체 선택 헬퍼 메서드
        /// </summary>
        private BlockReference SelectBlock(Editor ed, string prompt)
        {
            var peo = new PromptEntityOptions(prompt);
            peo.SetRejectMessage("\nBlock 객체만 선택할 수 있습니다.");
            peo.AddAllowedClass(typeof(BlockReference), true);

            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return null;

            using var tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
            var blockRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
            tr.Commit();
            return blockRef;
        }

        #endregion
    }

    /// <summary>
    /// Line과 Block 교차 정보를 담는 클래스 (원점 기준)
    /// </summary>
    public class LineBlockIntersectionInfo
    {
        public bool HasIntersection { get; set; }
        public Point3d[] IntersectionPoints { get; set; } = [];
        public double LineLength { get; set; }
        public string BlockName { get; set; } = "";
        public Point3d BlockOrigin { get; set; }
        public double DistanceToBlock { get; set; }
        public double Tolerance { get; set; }
    }


    public class LineVisibilityFilter
    {
        [CommandMethod("c_FILTERVISIBLELINES")]
        public void FilterVisibleLines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 1단계: 기준점 입력받기
                PromptPointOptions ppo = new PromptPointOptions("\n기준점(bp)을 선택하세요: ");
                PromptPointResult ppr = ed.GetPoint(ppo);

                if (ppr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n기준점 선택이 취소되었습니다.");
                    return;
                }

                Point3d basePoint = ppr.Value;

                // 2단계: Line들 선택
                var selectedLineIds = SelectLines(ed);
                if (selectedLineIds.Count == 0)
                {
                    ed.WriteMessage("\n선택된 라인이 없습니다.");
                    return;
                }

                // 3단계: Line 객체들 로드
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

                // 4단계: 필터링 수행
                var visibleLines = FilterLinesByVisibility(basePoint, lines);

                tr.Commit();

                // 5단계: 결과 출력
                ed.WriteMessage($"\n=== 필터링 결과 ===");
                ed.WriteMessage($"\n전체 라인: {lines.Count}개");
                ed.WriteMessage($"\n가시선 라인(교차 없음): {visibleLines.Count}개");
                ed.WriteMessage($"\n제거된 라인: {lines.Count - visibleLines.Count}개");

                // 6단계: 결과 시각화 (선택사항)
                HighlightVisibleLines(visibleLines, db);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 기준점에서 보이는 라인들만 필터링 (교차 검사)
        /// </summary>
        private List<Line> FilterLinesByVisibility(Point3d basePoint, List<Line> lines)
        {
            var visibleLines = new List<Line>();

            foreach (var targetLine in lines)
            {
                var len3 = targetLine.Length/3;  //start, end 지점에서 1/3 지점씩 검사
                // StartPoint 체크
                bool startPointBlocked = IsLineBlocked(basePoint, targetLine.GetPointAtDist(len3), targetLine, lines);

                // EndPoint 체크
                bool endPointBlocked = IsLineBlocked(basePoint, targetLine.GetPointAtDist(len3*2), targetLine, lines);

                // 둘 다 교차하지 않으면(false) 유지
                if (!startPointBlocked && !endPointBlocked)
                {
                    visibleLines.Add(targetLine);
                }
            }

            return visibleLines;
        }

        /// <summary>
        /// 기준점에서 목표점까지의 라인이 다른 라인들과 교차하는지 검사
        /// </summary>
        private bool IsLineBlocked(Point3d basePoint, Point3d targetPoint, Line targetLine, List<Line> allLines)
        {
            // 임시 라인 생성 (기준점 → 목표점)
            using (Line tempLine = new Line(basePoint, targetPoint))
            {
                foreach (var otherLine in allLines)
                {
                    // 자기 자신은 제외
                    if (otherLine.Handle == targetLine.Handle)
                        continue;

                    // 교차 검사
                    if (DoLinesIntersect(tempLine, otherLine))
                    {
                        return true; // 교차함 (차단됨)
                    }
                }
            }

            return false; // 교차 없음 (가시선 확보)
        }

        /// <summary>
        /// 두 라인이 교차하는지 검사 (IntersectWith 사용)
        /// </summary>
        private bool DoLinesIntersect(Line line1, Line line2)
        {
            try
            {
                Point3dCollection intersectionPoints = new Point3dCollection();

                // IntersectWith 메서드로 교차점 검사
                // Intersect.OnBothOperands: 두 선분이 실제로 교차하는 경우만
                line1.IntersectWith(
                    line2,
                    Intersect.OnBothOperands,
                    intersectionPoints,
                    IntPtr.Zero,
                    IntPtr.Zero
                );

                // 교차점이 있으면 true
                return intersectionPoints.Count > 0;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 라인 선택 메서드
        /// </summary>
        private List<ObjectId> SelectLines(Editor ed)
        {
            var lineIds = new List<ObjectId>();

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
        /// 가시선 라인들을 하이라이트 (시각화)
        /// </summary>
        private void HighlightVisibleLines(List<Line> visibleLines, Database db)
        {
            using var tr = db.TransactionManager.StartTransaction();

            // 녹색으로 하이라이트
            var highlightColor = Color.FromColorIndex(ColorMethod.ByAci, 3); // Green

            foreach (var line in visibleLines)
            {
                try
                {
                    // Handle로 원본 ObjectId 찾기
                    var dbObject = line.Database.GetObjectId(false, line.Handle, 0);

                    if (dbObject != ObjectId.Null)
                    {
                        if (tr.GetObject(dbObject, OpenMode.ForWrite) is Line writableLine)
                        {
                            writableLine.Color = highlightColor;
                        }
                    }
                }
                catch (System.Exception)
                {
                    continue;
                }
            }

            tr.Commit();
        }

        /// <summary>
        /// 통계 정보 출력 커맨드
        /// </summary>
        [CommandMethod("FILTERVISIBLELINES_STATS")]
        public void ShowFilterStatistics()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 기준점 입력
                PromptPointOptions ppo = new PromptPointOptions("\n기준점(bp)을 선택하세요: ");
                PromptPointResult ppr = ed.GetPoint(ppo);

                if (ppr.Status != PromptStatus.OK)
                    return;

                Point3d basePoint = ppr.Value;

                // 라인 선택
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

                // 필터링 및 상세 통계
                var visibleLines = new List<Line>();
                var blockedLines = new List<(Line line, string reason)>();

                foreach (var targetLine in lines)
                {
                    bool startBlocked = IsLineBlocked(basePoint, targetLine.StartPoint, targetLine, lines);
                    bool endBlocked = IsLineBlocked(basePoint, targetLine.EndPoint, targetLine, lines);

                    if (!startBlocked && !endBlocked)
                    {
                        visibleLines.Add(targetLine);
                    }
                    else
                    {
                        string reason = "";
                        if (startBlocked && endBlocked)
                            reason = "양쪽 끝점 모두 차단됨";
                        else if (startBlocked)
                            reason = "시작점 차단됨";
                        else
                            reason = "끝점 차단됨";

                        blockedLines.Add((targetLine, reason));
                    }
                }

                tr.Commit();

                // 상세 결과 출력
                ed.WriteMessage("\n");
                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\n       가시선 필터링 상세 통계");
                ed.WriteMessage("\n========================================");
                ed.WriteMessage($"\n기준점: X={basePoint.X:F2}, Y={basePoint.Y:F2}, Z={basePoint.Z:F2}");
                ed.WriteMessage($"\n전체 라인 수: {lines.Count}개");
                ed.WriteMessage($"\n가시선 라인: {visibleLines.Count}개 ({(double)visibleLines.Count / lines.Count * 100:F1}%)");
                ed.WriteMessage($"\n차단된 라인: {blockedLines.Count}개 ({(double)blockedLines.Count / lines.Count * 100:F1}%)");

                if (blockedLines.Count > 0)
                {
                    ed.WriteMessage("\n");
                    ed.WriteMessage("\n--- 차단된 라인 상세 ---");
                    for (int i = 0; i < Math.Min(10, blockedLines.Count); i++)
                    {
                        var item = blockedLines[i];
                        ed.WriteMessage($"\n라인 {i + 1}: {item.reason}");
                    }
                    if (blockedLines.Count > 10)
                    {
                        ed.WriteMessage($"\n... 외 {blockedLines.Count - 10}개");
                    }
                }

                ed.WriteMessage("\n========================================\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }

}
