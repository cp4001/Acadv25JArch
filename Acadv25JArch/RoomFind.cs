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
        [CommandMethod(COMMAND_NAME)]
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
        [CommandMethod(COMMAND_NAME+"Multi")]
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
                var searchPolygon = CreateSearchPolygonAroundLine(baseLine);

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
        [CommandMethod("INTERSECTIONSTATS")]
        public void ShowIntersectionStatistics()
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
        [CommandMethod("SHOWSEARCHAREA")]
        public void ShowSearchArea()
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
}
