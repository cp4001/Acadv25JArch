using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Colors;
using System;
using System.Collections.Generic;
using System.Linq;
using Gile.AutoCAD.R25.Geometry;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color; // KdTree 네임스페이스

namespace AutoCADKdTreeLineHighlighter
{
    public class KdTreeLineHighlighter
    {
        private const double BOX_SIZE = 2000.0;
        private const short RED_COLOR_INDEX = 1;

        /// <summary>
        /// KdTree를 이용한 고성능 Line 검색 및 색상 변경 메인 커맨드
        /// </summary>
        [CommandMethod("HighlightLinesKdTree")]
        public void HighlightLinesWithKdTreeCommand()
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

                // 2단계: 모든 Line 객체들 수집
                ed.WriteMessage("\nLine 객체들을 수집하고 KdTree를 구축하는 중...");

                var lines = CollectAllLines(db);
                if (lines.Count == 0)
                {
                    ed.WriteMessage("\n도면에 Line 객체를 찾을 수 없습니다.");
                    return;
                }

                ed.WriteMessage($"\n총 {lines.Count}개의 Line 객체를 발견했습니다.");

                // 3단계: KdTree 구축 (Line 객체 직접 사용)
                var kdTree = new KdTree<Line>(
                    lines,
                    line => GetLineMidPoint(line), // Line의 중점을 위치로 사용
                    2 // 2D 검색
                );

                ed.WriteMessage("\nKdTree 구축 완료.");

                // 4단계: 박스 범위 계산
                double halfSize = BOX_SIZE / 2.0;
                Point3d lowerLeft = new Point3d(
                    centerPoint.X - halfSize,
                    centerPoint.Y - halfSize,
                    centerPoint.Z
                );
                Point3d upperRight = new Point3d(
                    centerPoint.X + halfSize,
                    centerPoint.Y + halfSize,
                    centerPoint.Z
                );

                // 5단계: KdTree를 이용한 고속 범위 검색
                ed.WriteMessage("\n박스 범위 내 Line들을 검색하는 중...");

                List<Line> linesInRange = kdTree.GetBoxedRange(lowerLeft, upperRight);

                ed.WriteMessage($"\n박스 범위 내에서 {linesInRange.Count}개의 Line을 발견했습니다.");

                // 6단계: 추가 필터링 - 실제로 박스와 교차하는지 확인
                var filteredLines = FilterLinesIntersectingBox(linesInRange, lowerLeft, upperRight);

                ed.WriteMessage($"\n실제 박스와 교차하는 Line: {filteredLines.Count}개");

                // 7단계: 색상을 빨간색으로 변경
                int coloredCount = ChangeLinesToRed(db, filteredLines);

                // 8단계: 결과 출력
                ed.WriteMessage($"\n=== 작업 완료 ===");
                ed.WriteMessage($"\n전체 Line 수: {lines.Count}");
                ed.WriteMessage($"\n박스 범위 내 Line 수: {linesInRange.Count}");
                ed.WriteMessage($"\n실제 교차하는 Line 수: {filteredLines.Count}");
                ed.WriteMessage($"\n빨간색으로 변경된 Line 수: {coloredCount}");

            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// Line의 중점을 계산하는 헬퍼 함수
        /// </summary>
        private Point3d GetLineMidPoint(Line line)
        {
            return new Point3d(
                (line.StartPoint.X + line.EndPoint.X) / 2,
                (line.StartPoint.Y + line.EndPoint.Y) / 2,
                (line.StartPoint.Z + line.EndPoint.Z) / 2
            );
        }

        /// <summary>
        /// 도면의 모든 Line 객체들을 수집
        /// </summary>
        private List<Line> CollectAllLines(Database db)
        {
            List<Line> lines = new List<Line>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId objId in btr)
                    {
                        if (tr.GetObject(objId, OpenMode.ForRead) is Line line)
                        {
                            // Line 객체를 복사하여 Transaction 외부에서도 사용 가능하게 함
                            Line clonedLine = line.Clone() as Line;
                            if (clonedLine != null)
                            {
                                // ObjectId 정보를 저장하기 위해 XData 사용
                                clonedLine.SetDatabaseDefaults(db);
                                lines.Add(clonedLine);

                                // ObjectId를 찾기 위한 매핑 정보 저장 (Handle 사용)
                                lineHandleMap[clonedLine.Handle] = objId;
                            }
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

            return lines;
        }

        // Handle을 ObjectId로 매핑하는 딕셔너리
        private Dictionary<Handle, ObjectId> lineHandleMap = new Dictionary<Handle, ObjectId>();

        /// <summary>
        /// Line들이 실제로 박스와 교차하는지 정밀 검사
        /// </summary>
        private List<Line> FilterLinesIntersectingBox(List<Line> lines, Point3d lowerLeft, Point3d upperRight)
        {
            var result = new List<Line>();

            foreach (var line in lines)
            {
                if (IsLineIntersectingBox(line, lowerLeft, upperRight))
                {
                    result.Add(line);
                }
            }

            return result;
        }

        /// <summary>
        /// Line이 박스와 교차하는지 확인 (2D 기준)
        /// </summary>
        private bool IsLineIntersectingBox(Line line, Point3d lowerLeft, Point3d upperRight)
        {
            Point3d start = line.StartPoint;
            Point3d end = line.EndPoint;

            // 박스 경계
            double minX = lowerLeft.X;
            double minY = lowerLeft.Y;
            double maxX = upperRight.X;
            double maxY = upperRight.Y;

            // Line의 양 끝점이 모두 박스 한쪽에 있으면 교차하지 않음
            if ((start.X < minX && end.X < minX) || (start.X > maxX && end.X > maxX) ||
                (start.Y < minY && end.Y < minY) || (start.Y > maxY && end.Y > maxY))
            {
                return false;
            }

            // Line이 박스를 관통하거나 박스 내부에 있으면 교차
            return true;
        }

        /// <summary>
        /// Line들의 색상을 빨간색으로 변경
        /// </summary>
        private int ChangeLinesToRed(Database db, List<Line> lines)
        {
            int coloredCount = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Color redColor = Color.FromColorIndex(ColorMethod.ByAci, RED_COLOR_INDEX);

                    foreach (var line in lines)
                    {
                        try
                        {
                            // Handle을 통해 원본 ObjectId 찾기
                            if (lineHandleMap.TryGetValue(line.Handle, out ObjectId originalId))
                            {
                                Line originalLine = tr.GetObject(originalId, OpenMode.ForWrite) as Line;
                                if (originalLine != null)
                                {
                                    originalLine.Color = redColor;
                                    coloredCount++;
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            // 개별 Line 처리 실패 시 계속 진행
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

            return coloredCount;
        }

        /// <summary>
        /// 근거리 Line 검색 데모 (반경 기반)
        /// </summary>
        [CommandMethod("FindNearbyLines")]
        public void FindNearbyLinesCommand()
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

                // 반경 입력
                PromptDoubleOptions radiusOpts = new PromptDoubleOptions("\n검색 반경을 입력하세요: ");
                radiusOpts.DefaultValue = 1000.0;
                PromptDoubleResult radiusResult = ed.GetDouble(radiusOpts);

                if (radiusResult.Status != PromptStatus.OK)
                    return;

                Point3d centerPoint = pointResult.Value;
                double radius = radiusResult.Value;

                // Line들 수집 및 KdTree 구축
                var lines = CollectAllLines(db);
                var kdTree = new KdTree<Line>(lines, line => GetLineMidPoint(line), 2);

                // 반경 내 Line들 검색
                var nearbyLines = kdTree.GetNearestNeighbours(centerPoint, radius);

                // 색상 변경
                int coloredCount = ChangeLinesToRed(db, nearbyLines);

                ed.WriteMessage($"\n반경 {radius} 내에서 {coloredCount}개의 Line을 빨간색으로 변경했습니다.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 최근접 Line 검색 (가장 가까운 1개)
        /// </summary>
        [CommandMethod("FindNearestLine")]
        public void FindNearestLineCommand()
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

                // Line들 수집 및 KdTree 구축
                var lines = CollectAllLines(db);
                if (lines.Count == 0)
                {
                    ed.WriteMessage("\nLine을 찾을 수 없습니다.");
                    return;
                }

                var kdTree = new KdTree<Line>(lines, line => GetLineMidPoint(line), 2);

                // 가장 가까운 Line 검색
                var nearestLine = kdTree.GetNearestNeighbour(centerPoint);

                // 거리 계산
                Point3d nearestMidPoint = GetLineMidPoint(nearestLine);
                double distance = centerPoint.DistanceTo(nearestMidPoint);

                // 색상 변경
                var nearestList = new List<Line> { nearestLine };
                int coloredCount = ChangeLinesToRed(db, nearestList);

                ed.WriteMessage($"\n가장 가까운 Line을 빨간색으로 변경했습니다.");
                ed.WriteMessage($"\n거리: {distance:F2}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 성능 비교용: 기존 방식 (GetCrossingPolygon)
        /// </summary>
        [CommandMethod("HighlightLinesTraditional")]
        public void HighlightLinesTraditionalCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                var startTime = DateTime.Now;

                // 기준점 선택
                PromptPointOptions pointOpts = new PromptPointOptions("\n기준점을 선택하세요 (기존 방식): ");
                PromptPointResult pointResult = ed.GetPoint(pointOpts);

                if (pointResult.Status != PromptStatus.OK)
                    return;

                Point3d centerPoint = pointResult.Value;

                // 선택 박스 생성
                Point3dCollection selectionBox = CreateSelectionBox(centerPoint, BOX_SIZE);

                // 기존 방식으로 선택
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "LINE")
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                PromptSelectionResult selectionResult = ed.SelectCrossingPolygon(selectionBox, filter);

                if (selectionResult.Status == PromptStatus.OK && selectionResult.Value != null)
                {
                    ObjectId[] selectedIds = selectionResult.Value.GetObjectIds();

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        Color redColor = Color.FromColorIndex(ColorMethod.ByAci, RED_COLOR_INDEX);

                        foreach (ObjectId lineId in selectedIds)
                        {
                            Line line = tr.GetObject(lineId, OpenMode.ForWrite) as Line;
                            if (line != null)
                            {
                                line.Color = redColor;
                            }
                        }

                        tr.Commit();
                    }

                    var elapsed = DateTime.Now - startTime;
                    ed.WriteMessage($"\n기존 방식 완료: {selectedIds.Length}개 Line 처리, 소요시간: {elapsed.TotalMilliseconds:F2}ms");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 선택 박스 생성 (기존 방식용)
        /// </summary>
        private Point3dCollection CreateSelectionBox(Point3d centerPoint, double boxSize)
        {
            double halfSize = boxSize / 2.0;

            Point3dCollection polygon = new Point3dCollection();
            polygon.Add(new Point3d(centerPoint.X - halfSize, centerPoint.Y - halfSize, centerPoint.Z));
            polygon.Add(new Point3d(centerPoint.X + halfSize, centerPoint.Y - halfSize, centerPoint.Z));
            polygon.Add(new Point3d(centerPoint.X + halfSize, centerPoint.Y + halfSize, centerPoint.Z));
            polygon.Add(new Point3d(centerPoint.X - halfSize, centerPoint.Y + halfSize, centerPoint.Z));

            return polygon;
        }

        /// <summary>
        /// 성능 테스트 커맨드
        /// </summary>
        [CommandMethod("PerformanceTest")]
        public void PerformanceTestCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Line 수집
                ed.WriteMessage("\n성능 테스트를 시작합니다...");
                var lines = CollectAllLines(db);
                ed.WriteMessage($"\n총 {lines.Count}개의 Line을 발견했습니다.");

                if (lines.Count == 0)
                {
                    ed.WriteMessage("\n테스트할 Line이 없습니다.");
                    return;
                }

                // KdTree 구축 시간 측정
                var buildStart = DateTime.Now;
                var kdTree = new KdTree<Line>(lines, line => GetLineMidPoint(line), 2);
                var buildTime = DateTime.Now - buildStart;

                ed.WriteMessage($"\nKdTree 구축 시간: {buildTime.TotalMilliseconds:F2}ms");

                // 테스트 점
                Point3d testPoint = new Point3d(0, 0, 0);

                // 최근접 이웃 검색 시간 측정
                var searchStart = DateTime.Now;
                var nearest = kdTree.GetNearestNeighbour(testPoint);
                var searchTime = DateTime.Now - searchStart;

                ed.WriteMessage($"\n최근접 이웃 검색 시간: {searchTime.TotalMilliseconds:F6}ms");

                // 박스 범위 검색 시간 측정
                var boxStart = DateTime.Now;
                var inBox = kdTree.GetBoxedRange(
                    new Point3d(-1000, -1000, 0),
                    new Point3d(1000, 1000, 0)
                );
                var boxTime = DateTime.Now - boxStart;

                ed.WriteMessage($"\n박스 범위 검색 시간: {boxTime.TotalMilliseconds:F2}ms");
                ed.WriteMessage($"\n박스 내 Line 수: {inBox.Count}개");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 색상 복원 커맨드
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
