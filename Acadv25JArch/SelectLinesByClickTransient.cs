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
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Shapes;
using static System.Windows.Forms.LinkLabel;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
namespace AutoCADCommands
{
    public class SelectLinesByClickTransient
    {
        private const double SELECTION_RADIUS = 10000.0; // 선택 반경
        private const string COMMAND_NAME = "c_SELECTBYCLICK";
        private const string COMMAND_NAME1 = "c_SELECTBYCLICKFirst";
        /// <summary>
        /// 클릭한 점을 중심으로 반경 300 범위의 Line을 선택하고
        /// 파란색 실선(굵은 선)으로 Transient 그래픽 표시
        /// Enter를 누르면 종료
        /// </summary>
        [CommandMethod(COMMAND_NAME)]
        public void SelectLinesByClickPoint()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage($"\n=== {COMMAND_NAME} 시작 ===");
                ed.WriteMessage($"\n클릭한 점 기준 반경 {SELECTION_RADIUS} 범위의 Line을 선택합니다.");
                ed.WriteMessage("\n선택된 Line은 파란색 굵은 실선으로 표시됩니다.");
                ed.WriteMessage("\nEnter를 누르면 종료됩니다.\n");

                int clickCount = 0;
                var allSelectedLineIds = new HashSet<ObjectId>(); // 중복 방지
                var transientLines = new List<Line>(); // Transient 그래픽으로 표시할 Line들
                var transientManager = TransientManager.CurrentTransientManager;
                var intCol = new IntegerCollection(); // 빈 IntegerCollection

                // Line만 선택하도록 필터 설정
                TypedValue[] filterList = [
                    new TypedValue((int)DxfCode.Start, "LINE")
                ];
                var filter = new SelectionFilter(filterList);

                while (true)
                {
                    // 1단계: 사용자로부터 클릭 점 받기
                    var pointPrompt = new PromptPointOptions($"\n선택할 중심점을 클릭하세요 (Enter=종료): ")
                    {
                        AllowNone = true // Enter 허용
                    };

                    var pointResult = ed.GetPoint(pointPrompt);

                    // Enter 입력 시 종료
                    if (pointResult.Status == PromptStatus.Cancel || 
                        pointResult.Status == PromptStatus.None)
                    {
                        break; // while 루프 종료
                    }

                    if (pointResult.Status != PromptStatus.OK)
                        continue;

                    Point3d clickPoint = pointResult.Value;
                    clickCount++;

                    // 2단계: 클릭점 기준 반경 300의 정사각형 영역 계산
                    Point3d corner1 = new Point3d(
                        clickPoint.X - SELECTION_RADIUS,
                        clickPoint.Y - SELECTION_RADIUS,
                        clickPoint.Z
                    );

                    Point3d corner2 = new Point3d(
                        clickPoint.X + SELECTION_RADIUS,
                        clickPoint.Y + SELECTION_RADIUS,
                        clickPoint.Z
                    );

                    // 3단계: SelectCrossingWindow로 Line 선택
                    var selectionResult = ed.SelectCrossingWindow(corner1, corner2, filter);

                    // 4단계: 선택된 Line들을 Transient Graphics로 표시
                    if (selectionResult.Status == PromptStatus.OK && selectionResult.Value != null)
                    {
                        var selectedIds = selectionResult.Value.GetObjectIds();
                        int newLinesCount = 0;

                        using (var tr = db.TransactionManager.StartTransaction())
                        {
                            foreach (ObjectId id in selectedIds)
                            {
                                // 이미 선택된 Line은 건너뛰기 (중복 방지)
                                if (allSelectedLineIds.Contains(id))
                                    continue;

                                // 원본 Line 읽기 (ForRead만 사용 - 원본 수정 안 함!)
                                var originalLine = tr.GetObject(id, OpenMode.ForRead) as Line;
                                if (originalLine == null)
                                    continue;

                                // 가상 Line 생성 (DB에 추가하지 않음!)
                                var transientLine = new Line(
                                    originalLine.StartPoint,
                                    originalLine.EndPoint
                                );

                                // ✅ 중요: ColorIndex 직접 설정 (AutoCAD Blue = 5)
                                transientLine.ColorIndex = 5;
                                
                                // ✅ LineWeight 설정
                                transientLine.LineWeight = LineWeight.LineWeight030; // 0.30mm

                                // ✅ Layer를 "0"으로 설정 (DEFPOINTS 아님!)
                                transientLine.Layer = "0";

                                // ✅ Transient Graphics로 화면에 표시 (DirectTopmost 사용!)
                                transientManager.AddTransient(
                                    transientLine,
                                    TransientDrawingMode.DirectTopmost, // 최상위에 표시
                                    128,
                                    intCol
                                );

                                // 관리 목록에 추가
                                transientLines.Add(transientLine);
                                allSelectedLineIds.Add(id);
                                newLinesCount++;
                            }

                            tr.Commit();
                        }

                        ed.WriteMessage($"\n[클릭 #{clickCount}] 위치: ({clickPoint.X:F2}, {clickPoint.Y:F2})");
                        ed.WriteMessage($" → {newLinesCount}개 새로운 Line 선택됨 (누적: {allSelectedLineIds.Count}개)");

                        // ✅ 강제 화면 갱신
                        ed.UpdateScreen();
                        ed.Regen();
                    }
                    else
                    {
                        ed.WriteMessage($"\n[클릭 #{clickCount}] 위치: ({clickPoint.X:F2}, {clickPoint.Y:F2})");
                        ed.WriteMessage(" → 선택된 Line 없음");
                    }
                }

                // 5단계: 종료 - 모든 Transient Graphics 제거
                ed.WriteMessage($"\n\n=== 선택 종료 ===");
                ed.WriteMessage($"\n총 {clickCount}번 클릭했습니다.");
                ed.WriteMessage($"\n총 {allSelectedLineIds.Count}개의 Line이 선택되었습니다.");
                ed.WriteMessage("\n가상 그래픽을 제거합니다...");

                foreach (var transientLine in transientLines)
                {
                    try
                    {
                        transientManager.EraseTransient(transientLine, intCol);
                        transientLine.Dispose();
                    }
                    catch (System.Exception)
                    {
                        // 이미 제거된 경우 무시
                    }
                }

                // 화면 갱신
                ed.UpdateScreen();
                ed.Regen();
                ed.WriteMessage("\n가상 그래픽 제거 완료!");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
                ed.WriteMessage($"\n스택 트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 클릭한 점을 중심으로 반경 300 범위의 Line을 선택하고
        /// 파란색 실선(굵은 선)으로 Transient 그래픽 표시
        /// Enter를 누르면 종료
        /// </summary>
        [CommandMethod(COMMAND_NAME1)]
        public void SelectLinesByClickPointFirstEntity()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage($"\n=== {COMMAND_NAME} 시작 ===");
                ed.WriteMessage($"\n클릭한 점 기준 반경 {SELECTION_RADIUS} 범위의 Line을 선택합니다.");
                ed.WriteMessage("\n선택된 Line은 파란색 굵은 실선으로 표시됩니다.");
                ed.WriteMessage("\nEnter를 누르면 종료됩니다.\n");

                int clickCount = 0;
                var allSelectedLineIds = new HashSet<ObjectId>(); // 중복 방지
                var transientLines = new List<Line>(); // Transient 그래픽으로 표시할 Line들
                var transientManager = TransientManager.CurrentTransientManager;
                var intCol = new IntegerCollection(); // 빈 IntegerCollection

                // Line만 선택하도록 필터 설정
                TypedValue[] filterList = [
                    new TypedValue((int)DxfCode.Start, "LINE")
                ];
                var filter = new SelectionFilter(filterList);

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    while (true)
                    {
                        // 1단계: 사용자로부터 클릭 점 받기
                        var pointPrompt = new PromptPointOptions($"\n선택할 중심점을 클릭하세요 (Enter=종료): ")
                        {
                            AllowNone = true // Enter 허용
                        };

                        var pointResult = ed.GetPoint(pointPrompt);

                        // Enter 입력 시 종료
                        if (pointResult.Status == PromptStatus.Cancel ||
                            pointResult.Status == PromptStatus.None)
                        {
                            break; // while 루프 종료
                        }

                        if (pointResult.Status != PromptStatus.OK)
                            continue;

                        Point3d clickPoint = pointResult.Value;
                        clickCount++;


                        // click Point 로 부터  수평좌   line  생성 
                        var baseline = new Line(
                                clickPoint,
                                new Point3d(clickPoint.X - SELECTION_RADIUS, clickPoint.Y, clickPoint.Z)
                        );
                        //var sline = new Line(
                        //        clickPoint,
                        //        new Point3d(clickPoint.X, clickPoint.Y - SELECTION_RADIUS, clickPoint.Z)
                        //    );


                        //// 2단계: 클릭점 기준 반경 300의 정사각형 영역 계산
                        //Point3d corner1 = new Point3d(
                        //    clickPoint.X - SELECTION_RADIUS,
                        //    clickPoint.Y - SELECTION_RADIUS,
                        //    clickPoint.Z
                        //);

                        //Point3d corner2 = new Point3d(
                        //    clickPoint.X + SELECTION_RADIUS,
                        //    clickPoint.Y + SELECTION_RADIUS,
                        //    clickPoint.Z
                        //);

                        //// 3단계: SelectCrossingWindow로 Line 선택
                        //var selectionResult = ed.SelectCrossingWindow(corner1, corner2, filter);


                        // 3단계: Line에 교차되는 Entity 선택 
                        var lls = new List<ObjectId>();
                        
                        //var sel_ll = baseline.GetFirstLine(clickPoint);

                        // 회전 횟수 계산 (360도 채우기)
                        // 3단계: 회전 전 정보 저장
                        Point3d centerPoint = baseline.StartPoint;
                        Point3d originalEndPoint = baseline.EndPoint;
                        double originalLength = baseline.Length;
                        // 4단계: 회전 각도를 라디안으로 변환 (반시계 방향 = 양수)
                        var customAngle = 30.0;
                        double angleRadians = 30 * Math.PI / 180.0;
                        int rotationCount = (int)(360.0 / customAngle) - 1;
                        // 5단계: Z축 기준 회전 행렬 생성
                        Matrix3d rotationMatrix = Matrix3d.Rotation(
                            angleRadians,       // 회전 각도 (라디안) - 양수 = 반시계
                            Vector3d.ZAxis,     // 회전 축 (Z축)
                            centerPoint         // 회전 중심점 (StartPoint)
                        );
                        
                        for (int i = 1; i <= rotationCount; i++)
                        {
                            var sel_ll = baseline.GetFirstLine(clickPoint);
                            if (sel_ll != null) lls.Add(sel_ll.ObjectId);

                            // 6단계: EndPoint를 회전 변환
                            Point3d rotatedEndPoint = baseline.EndPoint.TransformBy(rotationMatrix);
                            // 7단계: Line의 EndPoint 업데이트
                            baseline.EndPoint = rotatedEndPoint;

                        }

                        //    sel_ll.Highlight();
                        //ed.UpdateScreen();

                        //CadFunc.LinesDrawGraphic(sel_ll, tr);   

                        //if (ent != null)
                        //{
                        //    ent.Highlight();
                        //    // 화면 업데이트
                        //    ed.UpdateScreen();
                        //}
                        //if (ent is Line l)
                        //{
                        //    lls.Add(l.ObjectId);
                        //}

                        //ent = sline.GetFirstLine(clickPoint);
                        //if (ent is Line l1)
                        //{
                        //    lls.Add(l1.ObjectId);
                        //}



                        //var selectionResult = ed.SelectCrossingWindow(corner1, corner2, filter);


                        //// 4단계: 선택된 Line들을 Transient Graphics로 표시
                        //if (lls.Count >= 1)
                        //{
                        //    //var selectedIds = selectionResult.Value.GetObjectIds();
                        //    int newLinesCount = 0;

                        lls = lls.Distinct().ToList();
                        foreach (var id in lls)
                        {
                            // 이미 선택된 Line은 건너뛰기 (중복 방지)
                            if (allSelectedLineIds.Contains(id))
                                continue;

                            // 원본 Line 읽기 (ForRead만 사용 - 원본 수정 안 함!)
                            var originalLine = tr.GetObject(id, OpenMode.ForRead) as Line;
                            if (originalLine == null)
                                continue;

                            // 가상 Line 생성 (DB에 추가하지 않음!)
                            var transientLine = new Line(
                                originalLine.StartPoint,
                                originalLine.EndPoint
                            );

                            // ✅ 중요: ColorIndex 직접 설정 (AutoCAD Blue = 5)
                            transientLine.ColorIndex = 5;

                            // ✅ LineWeight 설정
                            transientLine.LineWeight = LineWeight.LineWeight050; // 0.30mm

                            // ✅ Layer를 "0"으로 설정 (DEFPOINTS 아님!)
                            transientLine.Layer = "0";

                            // ✅ Transient Graphics로 화면에 표시 (DirectTopmost 사용!)
                            transientManager.AddTransient(
                                transientLine,
                                TransientDrawingMode.DirectTopmost, // 최상위에 표시
                                128,
                                intCol
                            );

                            // 관리 목록에 추가
                            transientLines.Add(transientLine);
                            allSelectedLineIds.Add(id);
                            //newLinesCount++;
                        }

                        ed.UpdateScreen();

                        //    ed.WriteMessage($"\n[클릭 #{clickCount}] 위치: ({clickPoint.X:F2}, {clickPoint.Y:F2})");
                        //    ed.WriteMessage($" → {newLinesCount}개 새로운 Line 선택됨 (누적: {allSelectedLineIds.Count}개)");

                        //    // ✅ 강제 화면 갱신
                        //    ed.UpdateScreen();
                        //    ed.Regen();
                        //}
                        //else
                        //{
                        //    ed.WriteMessage($"\n[클릭 #{clickCount}] 위치: ({clickPoint.X:F2}, {clickPoint.Y:F2})");
                        //    ed.WriteMessage(" → 선택된 Line 없음");
                        //}

                    }

                    // 5단계: 종료 - 모든 Transient Graphics 제거
                    //ed.WriteMessage($"\n\n=== 선택 종료 ===");
                    //ed.WriteMessage($"\n총 {clickCount}번 클릭했습니다.");
                    //ed.WriteMessage($"\n총 {allSelectedLineIds.Count}개의 Line이 선택되었습니다.");
                    //ed.WriteMessage("\n가상 그래픽을 제거합니다...");

                    //foreach (var transientLine in transientLines)
                    //{
                    //    try
                    //    {
                    //        transientManager.EraseTransient(transientLine, intCol);
                    //        transientLine.Dispose();
                    //    }
                    //    catch (System.Exception)
                    //    {
                    //        // 이미 제거된 경우 무시
                    //    }
                    //}
                    tr.Commit();
                    // 화면 갱신
                    ed.UpdateScreen();
                    ed.Regen();
                    ed.WriteMessage("\n가상 그래픽 제거 완료!");
                }
                
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
                ed.WriteMessage($"\n스택 트레이스: {ex.StackTrace}");
            }
        }
        


        /// <summary>
        /// 테스트용: 간단한 Transient Line 표시
        /// </summary>
        [CommandMethod("TESTTR")]
        public void TestTransient()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== Transient 테스트 시작 ===");

                // 간단한 테스트 Line 생성
                Line testLine = new Line(
                    new Point3d(0, 0, 0),
                    new Point3d(1000, 1000, 0)
                );

                // 속성 설정
                testLine.ColorIndex = 1; // Red
                testLine.LineWeight = LineWeight.LineWeight050; // 0.50mm
                testLine.Layer = "0";

                // Transient로 표시
                var tm = TransientManager.CurrentTransientManager;
                var intCol = new IntegerCollection();

                tm.AddTransient(
                    testLine,
                    TransientDrawingMode.DirectTopmost,
                    128,
                    intCol
                );

                ed.WriteMessage("\n빨간색 테스트 Line이 (0,0)에서 (1000,1000)까지 표시됩니다.");
                ed.WriteMessage("\nEnter를 눌러 제거하세요...");

                ed.GetString("\n");

                // 제거
                tm.EraseTransient(testLine, intCol);
                testLine.Dispose();

                ed.UpdateScreen();
                ed.WriteMessage("\n테스트 Line 제거 완료!");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 Transient Graphics를 강제로 제거하는 유틸리티 명령
        /// </summary>
        [CommandMethod("CLEARTRANSIENTS")]
        public void ClearAllTransients()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                var transientManager = TransientManager.CurrentTransientManager;
                
                // 모든 Transient 제거
                transientManager.EraseTransients(
                    TransientDrawingMode.DirectTopmost, 
                    128, 
                    new IntegerCollection()
                );
                
                ed.WriteMessage("\n모든 Transient Graphics가 제거되었습니다.");
                ed.UpdateScreen();
                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 선택된 Line들의 정보를 출력하는 보조 명령
        /// </summary>
        [CommandMethod("SELECTBYCLICK_INFO")]
        public void ShowSelectionInfo()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 현재 선택된 객체 가져오기
                var selectionResult = ed.SelectImplied();
                
                if (selectionResult.Status != PromptStatus.OK || selectionResult.Value == null)
                {
                    ed.WriteMessage("\n선택된 객체가 없습니다.");
                    return;
                }

                using var tr = db.TransactionManager.StartTransaction();

                var selectedIds = selectionResult.Value.GetObjectIds();
                var lines = selectedIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as Line)
                    .Where(line => line != null)
                    .ToList();

                if (lines.Count == 0)
                {
                    ed.WriteMessage("\n선택된 Line이 없습니다.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage($"\n=== 선택된 Line 정보 ===");
                ed.WriteMessage($"\n총 {lines.Count}개의 Line이 선택되었습니다.\n");

                for (int i = 0; i < lines.Count && i < 10; i++) // 최대 10개만 표시
                {
                    var line = lines[i];
                    double length = line.StartPoint.DistanceTo(line.EndPoint);
                    
                    ed.WriteMessage($"\nLine {i + 1}:");
                    ed.WriteMessage($"  시작점: ({line.StartPoint.X:F2}, {line.StartPoint.Y:F2})");
                    ed.WriteMessage($"  끝점: ({line.EndPoint.X:F2}, {line.EndPoint.Y:F2})");
                    ed.WriteMessage($"  길이: {length:F2}");
                    ed.WriteMessage($"  색상: {line.ColorIndex}");
                }

                if (lines.Count > 10)
                {
                    ed.WriteMessage($"\n... 외 {lines.Count - 10}개 Line");
                }

                tr.Commit();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }

    public class TempGraphicLayerCommands
    {
        private const string TEMP_LAYER_NAME = "tempGraphic";

        /// <summary>
        /// tempGraphic 레이어에 임시 Line을 생성합니다.
        /// 레이어가 없으면 생성하고, 있으면 Display On만 시킵니다.
        /// </summary>
        [CommandMethod("DRAWTEMPLINE")]
        public void DrawTemporaryLine()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Step 1: tempGraphic 레이어 확인 및 생성/활성화
                    EnsureTempGraphicLayer(tr, db);

                    tr.Commit();
                }

                // Step 2: 첫 번째 점 입력
                PromptPointOptions ppo1 = new PromptPointOptions("\n첫 번째 점을 지정하세요: ");
                PromptPointResult ppr1 = ed.GetPoint(ppo1);

                if (ppr1.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n명령이 취소되었습니다.");
                    return;
                }

                Point3d pt1 = ppr1.Value;

                // Step 3: 두 번째 점 입력
                PromptPointOptions ppo2 = new PromptPointOptions("\n두 번째 점을 지정하세요: ")
                {
                    UseBasePoint = true,
                    BasePoint = pt1,
                    UseDashedLine = true
                };

                PromptPointResult ppr2 = ed.GetPoint(ppo2);

                if (ppr2.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n명령이 취소되었습니다.");
                    return;
                }

                Point3d pt2 = ppr2.Value;

                // Step 4: Line 생성 및 tempGraphic 레이어에 추가
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;

                    // Line 생성
                    using (Line newLine = new Line(pt1, pt2))
                    {
                        // tempGraphic 레이어 할당
                        newLine.Layer = TEMP_LAYER_NAME;

                        // 빨간색으로 표시
                        newLine.ColorIndex = 1;

                        // Database에 추가
                        btr.AppendEntity(newLine);
                        tr.AddNewlyCreatedDBObject(newLine, true);

                        ed.WriteMessage($"\ntempGraphic 레이어에 Line이 생성되었습니다. (길이: {newLine.Length:F3})");
                    }

                    tr.Commit();
                }

                ed.WriteMessage("\n제거하려면 CLEARTEMPLINES 명령을 실행하세요.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// tempGraphic 레이어의 모든 Entity를 제거합니다.
        /// </summary>
        [CommandMethod("CLEARTEMPLINES")]
        public void ClearTemporaryLines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Step 1: tempGraphic 레이어 존재 확인
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (!lt.Has(TEMP_LAYER_NAME))
                    {
                        ed.WriteMessage($"\n{TEMP_LAYER_NAME} 레이어가 존재하지 않습니다.");
                        tr.Commit();
                        return;
                    }

                    // Step 2: ModelSpace의 모든 Entity 순회
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForRead) as BlockTableRecord;

                    int deleteCount = 0;

                    // Step 3: tempGraphic 레이어의 Entity만 필터링하여 삭제
                    foreach (ObjectId objId in btr)
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                        if (ent != null && ent.Layer == TEMP_LAYER_NAME)
                        {
                            // Step 4: Erase() 호출
                            ent.UpgradeOpen();
                            ent.Erase();
                            deleteCount++;
                        }
                    }

                    tr.Commit();

                    ed.WriteMessage($"\n{deleteCount}개의 임시 객체가 제거되었습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 연속으로 여러 개의 임시 Line을 생성합니다.
        /// </summary>
        [CommandMethod("DRAWTEMPLINES")]
        public void DrawMultipleTemporaryLines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // tempGraphic 레이어 확인 및 생성
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    EnsureTempGraphicLayer(tr, db);
                    tr.Commit();
                }

                int count = 0;

                while (true)
                {
                    // 첫 번째 점 입력
                    PromptPointOptions ppo1 = new PromptPointOptions(
                        count == 0
                        ? "\n첫 번째 점을 지정하세요 (Enter로 종료): "
                        : "\n다음 Line의 첫 번째 점을 지정하세요 (Enter로 종료): "
                    );
                    ppo1.AllowNone = true;

                    PromptPointResult ppr1 = ed.GetPoint(ppo1);

                    if (ppr1.Status == PromptStatus.None || ppr1.Status != PromptStatus.OK)
                    {
                        if (count > 0)
                            ed.WriteMessage($"\n총 {count}개의 임시 Line이 생성되었습니다.");
                        break;
                    }

                    Point3d pt1 = ppr1.Value;

                    // 두 번째 점 입력
                    PromptPointOptions ppo2 = new PromptPointOptions("\n두 번째 점을 지정하세요: ")
                    {
                        UseBasePoint = true,
                        BasePoint = pt1,
                        UseDashedLine = true
                    };

                    PromptPointResult ppr2 = ed.GetPoint(ppo2);

                    if (ppr2.Status != PromptStatus.OK)
                        break;

                    Point3d pt2 = ppr2.Value;

                    // Line 생성 및 추가
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace],
                            OpenMode.ForWrite) as BlockTableRecord;

                        using (Line newLine = new Line(pt1, pt2))
                        {
                            newLine.Layer = TEMP_LAYER_NAME;
                            newLine.ColorIndex = 1;

                            btr.AppendEntity(newLine);
                            tr.AddNewlyCreatedDBObject(newLine, true);

                            count++;
                            ed.WriteMessage($"\nLine #{count} 생성 (길이: {newLine.Length:F3})");
                        }

                        tr.Commit();
                    }
                }

                if (count > 0)
                    ed.WriteMessage("\n제거하려면 CLEARTEMPLINES 명령을 실행하세요.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// tempGraphic 레이어가 존재하는지 확인하고, 없으면 생성, 있으면 Display On 시킵니다.
        /// </summary>
        private void EnsureTempGraphicLayer(Transaction tr, Database db)
        {
            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

            if (lt.Has(TEMP_LAYER_NAME))
            {
                // 레이어가 이미 존재하면 Display On만 확인
                LayerTableRecord ltr = tr.GetObject(lt[TEMP_LAYER_NAME], OpenMode.ForWrite) as LayerTableRecord;

                if (ltr.IsOff)
                {
                    ltr.IsOff = false;
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                        $"\n{TEMP_LAYER_NAME} 레이어가 활성화되었습니다.");
                }
            }
            else
            {
                // 레이어가 없으면 새로 생성
                lt.UpgradeOpen();

                using (LayerTableRecord ltr = new LayerTableRecord())
                {
                    ltr.Name = TEMP_LAYER_NAME;
                    ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                        Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1); // 빨간색

                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);

                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                        $"\n{TEMP_LAYER_NAME} 레이어가 생성되었습니다.");
                }
            }
        }

        /// <summary>
        /// tempGraphic 레이어의 Entity 개수를 확인합니다.
        /// </summary>
        [CommandMethod("COUNTTEMPLINES")]
        public void CountTemporaryLines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    if (!lt.Has(TEMP_LAYER_NAME))
                    {
                        ed.WriteMessage($"\n{TEMP_LAYER_NAME} 레이어가 존재하지 않습니다.");
                        tr.Commit();
                        return;
                    }

                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForRead) as BlockTableRecord;

                    int count = 0;

                    foreach (ObjectId objId in btr)
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;

                        if (ent != null && ent.Layer == TEMP_LAYER_NAME)
                        {
                            count++;
                        }
                    }

                    tr.Commit();

                    ed.WriteMessage($"\n{TEMP_LAYER_NAME} 레이어에 {count}개의 객체가 있습니다.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }
    }
}
