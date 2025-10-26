using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System;
using System.Windows.Controls;
using System.Windows.Shapes;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace AutoCADMultiEntityOverrule
{
    /// <summary>
    /// SetXDataFilter를 사용하여 여러 Entity 타입 지원
    /// - Line, Polyline: 색상을 Red로 변경
    /// - BlockReference: Block의 회전 각도를 고려한 GeometryExtents 박스 표시 (Cyan, LineWeight 30)
    /// - 모든 Entity 중앙에 "Aach" 텍스트 표시
    /// </summary>
    public class XDataFilterDrawOverrule : DrawableOverrule
    {
        private static XDataFilterDrawOverrule _instance;
        private const string XDATA_REGAPP_NAME = "Arch";
        public static bool IsRegistered => (_instance != null);

        public static void Register()
        {
            if (_instance == null)
            {
                _instance = new XDataFilterDrawOverrule();

                // Line, Polyline, BlockReference에 대해 Overrule 등록
                RXClass lineClass = RXObject.GetClass(typeof(Line));
                RXClass polylineClass = RXObject.GetClass(typeof(Polyline));
                RXClass blockRefClass = RXObject.GetClass(typeof(BlockReference));

                // 각 클래스에 Overrule 추가
                Overrule.AddOverrule(lineClass, _instance, false);
                Overrule.AddOverrule(polylineClass, _instance, false);
                Overrule.AddOverrule(blockRefClass, _instance, false);

                // 핵심: SetXDataFilter 설정
                _instance.SetXDataFilter(XDATA_REGAPP_NAME);

                // Overruling 활성화
                Overrule.Overruling = true;
                //IsRegistered = true;

                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\n========================================");
                    doc.Editor.WriteMessage("\n[XDataFilter] Multi-Entity Overrule 등록 완료");
                    doc.Editor.WriteMessage("\n========================================");
                    doc.Editor.WriteMessage($"\n✓ 대상: Line, Polyline, BlockReference");
                    doc.Editor.WriteMessage($"\n✓ Xdata 필터: '{XDATA_REGAPP_NAME}'");
                    doc.Editor.WriteMessage("\n✓ Line/Polyline: Red 색상으로 변경");
                    doc.Editor.WriteMessage("\n✓ Block: 회전 각도를 고려한 GeometryExtents 박스 표시 (Cyan, LineWeight 30)");
                    doc.Editor.WriteMessage("\n✓ 모든 Entity 중앙에 'Aach' 표시");
                    doc.Editor.WriteMessage("\n========================================");
                }
            }
        }

        public static void Unregister()
        {
            if (_instance != null)
            {
                RXClass lineClass = RXObject.GetClass(typeof(Line));
                RXClass polylineClass = RXObject.GetClass(typeof(Polyline));
                RXClass blockRefClass = RXObject.GetClass(typeof(BlockReference));

                // Overrule 제거
                Overrule.RemoveOverrule(lineClass, _instance);
                Overrule.RemoveOverrule(polylineClass, _instance);
                Overrule.RemoveOverrule(blockRefClass, _instance);

                _instance = null;
                //IsRegistered = false;

                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\n[XDataFilter] Multi-Entity Overrule 제거 완료");
                }
            }
        }

        public override bool IsApplicable(RXObject overruledSubject)
        {
            // SetXDataFilter가 이미 필터링했으므로
            // 여기 도달한 객체는 모두 Xdata가 있는 객체임
            Entity entity = overruledSubject as Entity;
            if (entity == null)
                return false;

            if (entity.ObjectId.IsNull || entity.ObjectId.IsErased)
                return false;

            return true;
        }

        /// <summary>
        /// WorldDraw - Entity 타입별로 다른 처리
        /// </summary>
        public override bool WorldDraw(Drawable drawable, WorldDraw wd)
        {
            Entity entity = drawable as Entity;
            if (entity == null)
                return base.WorldDraw(drawable, wd);

            try
            {
                // Entity 타입별 처리
                if (entity is Line line)
                {
                    DrawLine(line, wd);
                }
                else if (entity is Polyline polyline)
                {
                    DrawPolyline(polyline, wd);
                }
                else if (entity is BlockReference blockRef)
                {
                    DrawBlockReference(blockRef, wd);
                }
                else
                {
                    // 기타 Entity는 기본 그리기
                    return base.WorldDraw(drawable, wd);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WorldDraw Error: {ex.Message}");
                return base.WorldDraw(drawable, wd);
            }
        }

        /// <summary>
        /// Line을 빨간색으로 그리고 중앙에 "Aach" 표시
        /// </summary>
        private void DrawLine(Line line, WorldDraw wd)
        {
            // 원본 색상 저장
            short originalColor = wd.SubEntityTraits.Color;

            // 빨간색으로 설정
            wd.SubEntityTraits.Color = 90; // AutoCAD ACI에서 1은 Red

            // Line 그리기
            wd.Geometry.WorldLine(line.StartPoint, line.EndPoint);

            //// 원래 색상 복원
            //wd.SubEntityTraits.Color = originalColor;

            // 중심점 계산
            Point3d centerPoint = new Point3d(
                (line.StartPoint.X + line.EndPoint.X) / 2,
                (line.StartPoint.Y + line.EndPoint.Y) / 2,
                (line.StartPoint.Z + line.EndPoint.Z) / 2
            );

            // "Aach" 텍스트 표시
            DrawTextAtCenter(centerPoint, line.StartPoint, line.EndPoint, wd);
        }

        /// <summary>
        /// Polyline을 빨간색으로 그리고 중앙에 "Aach" 표시
        /// </summary>
        private void DrawPolyline(Polyline polyline, WorldDraw wd)
        {
            // 원본 색상 저장
            short originalColor = wd.SubEntityTraits.Color;

            // 빨간색으로 설정
            wd.SubEntityTraits.Color = 30; // Red
            wd.SubEntityTraits.LineWeight = LineWeight.LineWeight030;

            // Polyline의 각 세그먼트 그리기
            int numVertices = polyline.NumberOfVertices;
            for (int i = 0; i < numVertices; i++)
            {
                Point3d startPoint = polyline.GetPoint3dAt(i);
                Point3d endPoint = polyline.GetPoint3dAt((i + 1) % numVertices);

                // 닫힌 폴리라인이 아니고 마지막 세그먼트면 건너뛰기
                if (!polyline.Closed && i == numVertices - 1)
                    break;

                wd.Geometry.WorldLine(startPoint, endPoint);
            }

            // 원래 색상 복원
            wd.SubEntityTraits.Color = originalColor;

            // Polyline의 중심점 계산 (GeometricExtents 사용)
            Point3d centerPoint = GetEntityCenter(polyline);

            // "Aach" 텍스트 표시 (수평으로)
            DrawTextAtCenter(centerPoint, wd);
        }

        /// <summary>
        /// BlockReference의 GeometryExtents 박스를 Block의 회전 각도를 고려하여 Cyan 색상, LineWeight 30으로 그리기
        /// </summary>
        private void DrawBlockReference(BlockReference blockRef, WorldDraw wd)
        {
            try
            {
                // 기본 블록 그리기
                base.WorldDraw(blockRef, wd);

                //// Block의 회전 각도 가져오기
                //double rotation = blockRef.Rotation;
                //Point3d position = blockRef.Position;

                //// 회전하지 않은 상태에서 GeometryExtents 계산을 위한 임시 변환
                //// Block의 변환 행렬 생성 (회전 제외)
                //Matrix3d transform = Matrix3d.Identity;

                //// Scale 적용
                //transform = Matrix3d.Scaling(blockRef.ScaleFactors.X, position);

                //// 회전 각도가 0인 상태로 extents 계산
                //// BlockReference를 임시로 회전 각도 0으로 만들어 extents를 얻는 대신
                //// 현재 extents를 가져와서 역회전 적용
                //Extents3d extents = blockRef.GeometricExtents;

                //// Block의 중심점 계산 (회전된 상태)
                //Point3d center = new Point3d(
                //    (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                //    (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
                //    (extents.MinPoint.Z + extents.MaxPoint.Z) / 2
                //);

                //// Block의 로컬 좌표계에서 박스 크기 계산
                //// Position을 중심으로 역회전하여 회전하지 않은 상태의 extents를 구함
                //Matrix3d inverseRotation = Matrix3d.Rotation(-rotation, Vector3d.ZAxis, position);

                //Point3d minPtLocal = extents.MinPoint.TransformBy(inverseRotation);
                //Point3d maxPtLocal = extents.MaxPoint.TransformBy(inverseRotation);

                //// 회전하지 않은 상태의 박스 4개 모서리 점
                //Point3d p1 = new Point3d(minPtLocal.X, minPtLocal.Y, minPtLocal.Z);  // 좌하
                //Point3d p2 = new Point3d(maxPtLocal.X, minPtLocal.Y, minPtLocal.Z);  // 우하
                //Point3d p3 = new Point3d(maxPtLocal.X, maxPtLocal.Y, minPtLocal.Z);  // 우상
                //Point3d p4 = new Point3d(minPtLocal.X, maxPtLocal.Y, minPtLocal.Z);  // 좌상

                //// Block의 회전 각도를 다시 적용하여 점들을 회전
                //Matrix3d rotationMatrix = Matrix3d.Rotation(rotation, Vector3d.ZAxis, position);

                //Point3d p1Rotated = p1.TransformBy(rotationMatrix);
                //Point3d p2Rotated = p2.TransformBy(rotationMatrix);
                //Point3d p3Rotated = p3.TransformBy(rotationMatrix);
                //Point3d p4Rotated = p4.TransformBy(rotationMatrix);

                var polyline = blockRef.GetPoly1();

                // 원본 속성 저장
                short originalColor = wd.SubEntityTraits.Color;
                LineWeight originalLineWeight = wd.SubEntityTraits.LineWeight;

                // Cyan 색상(4)과 LineWeight 30 설정
                var type = JXdata.GetXdata(blockRef, "Type");
                if(type =="Window") wd.SubEntityTraits.Color = 50; 
                if(type == "Door") wd.SubEntityTraits.Color = 130; 
                //wd.SubEntityTraits.Color = 4; // Cyan
                wd.SubEntityTraits.LineWeight = LineWeight.LineWeight030;

                // Polyline의 각 세그먼트 그리기
                int numVertices = polyline.NumberOfVertices;
                for (int i = 0; i < numVertices; i++)
                {
                    Point3d startPoint = polyline.GetPoint3dAt(i);
                    Point3d endPoint = polyline.GetPoint3dAt((i + 1) % numVertices);

                    // 닫힌 폴리라인이 아니고 마지막 세그먼트면 건너뛰기
                    if (!polyline.Closed && i == numVertices - 1)
                        break;

                    wd.Geometry.WorldLine(startPoint, endPoint);
                }




                //// 회전된 박스 그리기 (4개의 선)
                //wd.Geometry.WorldLine(p1Rotated, p2Rotated);
                //wd.Geometry.WorldLine(p2Rotated, p3Rotated);
                //wd.Geometry.WorldLine(p3Rotated, p4Rotated);
                //wd.Geometry.WorldLine(p4Rotated, p1Rotated);

                ////// 원래 속성 복원
                ////wd.SubEntityTraits.Color = originalColor;
                ////wd.SubEntityTraits.LineWeight = originalLineWeight;

                //// 박스 중심점 (회전된 상태의 중심점 사용)
                //Point3d centerPoint = new Point3d(
                //    (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                //    (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
                //    (extents.MinPoint.Z + extents.MaxPoint.Z) / 2
                //);

                // "Aach" 텍스트 표시 (수평으로)
                DrawTextAtCenter(polyline.GetEntiyGeoCenter(), wd);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DrawBlockReference Error: {ex.Message}");
                base.WorldDraw(blockRef, wd);
            }
        }

        /// <summary>
        /// Entity의 중심점 계산 (GeometricExtents 사용)
        /// </summary>
        private Point3d GetEntityCenter(Entity entity)
        {
            try
            {
                Extents3d extents = entity.GeometricExtents;
                return new Point3d(
                    (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                    (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
                    (extents.MinPoint.Z + extents.MaxPoint.Z) / 2
                );
            }
            catch
            {
                return Point3d.Origin;
            }
        }

        /// <summary>
        /// 중심점에 "Aach" 텍스트 표시 (Line용 - 각도 계산)
        /// </summary>
        private void DrawTextAtCenter(Point3d centerPoint, Point3d startPoint, Point3d endPoint, WorldDraw wd)
        {
            Vector3d direction = (endPoint - startPoint).GetNormal();

            // 수직 방향 계산
            Vector3d perpendicular = new Vector3d(-direction.Y, direction.X, 0);
            if (perpendicular.Length > 0.0001)
                perpendicular = perpendicular.GetNormal();
            else
                perpendicular = Vector3d.YAxis;

            double textHeight = 2.5;
            Point3d textPosition = centerPoint + perpendicular * textHeight * 0.5;

            // 텍스트 각도 계산
            double angle = Math.Atan2(direction.Y, direction.X);
            if (angle > Math.PI / 2 || angle < -Math.PI / 2)
                angle += Math.PI;

            using (MText mtext = new MText())
            {
                mtext.SetDatabaseDefaults();
                mtext.Location = textPosition;
                mtext.TextHeight = textHeight;
                mtext.Contents = "Aach";
                mtext.Rotation = angle;
                mtext.Attachment = AttachmentPoint.MiddleCenter;
                mtext.Color = Color.FromRgb(255, 0, 0);
                mtext.WorldDraw(wd);
            }
        }

        /// <summary>
        /// 중심점에 "Aach" 텍스트 표시 (Polyline, Block용 - 수평)
        /// </summary>
        private void DrawTextAtCenter(Point3d centerPoint, WorldDraw wd)
        {
            double textHeight = 2.5;

            using (MText mtext = new MText())
            {
                mtext.SetDatabaseDefaults();
                mtext.Location = centerPoint;
                mtext.TextHeight = textHeight;
                mtext.Contents = "Aach";
                mtext.Rotation = 0;  // 수평
                mtext.Attachment = AttachmentPoint.MiddleCenter;
                mtext.Color = Color.FromRgb(255, 0, 0);
                mtext.WorldDraw(wd);
            }
        }
    }






    /// <summary>
    /// 명령어 클래스
    /// </summary>
    public class XDataFilterCommands
    {
        private const string XDATA_REGAPP_NAME = "Arch";


        [CommandMethod("aag")] // Wire Graphic
        public  void ToggleOverrule()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            // Initialize Overrule if first time run
            if (XDataFilterDrawOverrule.IsRegistered == false)
            {
                
                RegisterXdataApp(XDATA_REGAPP_NAME);
                XDataFilterDrawOverrule.Register();
                doc.Editor.Regen();
            }
            else 
            {
                //Turn Overruling off
                XDataFilterDrawOverrule.Unregister();
                ed.WriteMessage("\nSetXDataFilter Overrule이 제거되었습니다.");
                doc.Editor.Regen();

            }
        }

        [CommandMethod("REGISTERXDATAFILTER")]
        public void RegisterXDataFilter()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                RegisterXdataApp(XDATA_REGAPP_NAME);

                XDataFilterDrawOverrule.Register();

                ed.WriteMessage("\n[Multi-Entity] SetXDataFilter 방식 활성화!");
                ed.WriteMessage("\n✓ Line/Polyline: Red 색상으로 표시");
                ed.WriteMessage("\n✓ Block: 회전 각도를 고려한 GeometryExtents 박스 표시 (Cyan, LineWeight 30)");
                ed.WriteMessage("\n✓ 모든 대상 중앙에 'Aach' 표시");

                doc.Editor.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }

        [CommandMethod("UNREGISTERXDATAFILTER")]
        public void UnregisterXDataFilter()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                XDataFilterDrawOverrule.Unregister();
                ed.WriteMessage("\nSetXDataFilter Overrule이 제거되었습니다.");
                doc.Editor.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }

        [CommandMethod("ADDARCHXDATA")]
        public void AddArchXdata()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                RegisterXdataApp(XDATA_REGAPP_NAME);

                // Line, Polyline, BlockReference 모두 선택 가능
                PromptEntityOptions peo = new PromptEntityOptions("\nEntity 선택 (Line/Polyline/Block): ");
                peo.SetRejectMessage("\nLine, Polyline, Block만 선택할 수 있습니다.");
                peo.AddAllowedClass(typeof(Line), true);
                peo.AddAllowedClass(typeof(Polyline), true);
                peo.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity entity = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Entity;
                    if (entity != null)
                    {
                        ResultBuffer rb = new ResultBuffer(
                            new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDATA_REGAPP_NAME),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, "EntityDisplay")
                        );
                        entity.XData = rb;
                        rb.Dispose();

                        string entityType = entity.GetType().Name;
                        ed.WriteMessage($"\n'{XDATA_REGAPP_NAME}' Xdata가 {entityType}에 추가되었습니다.");

                        if (entity is BlockReference)
                            ed.WriteMessage("\n이제 이 Block은 회전 각도를 고려한 GeometryExtents 박스(Cyan, LineWeight 30)로 표시됩니다!");
                        else
                            ed.WriteMessage("\n이제 이 Entity는 Red 색상으로 표시됩니다!");
                    }

                    tr.Commit();
                }

                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }

        [CommandMethod("ADDARCHXDATABATCH")]
        public void AddArchXdataBatch()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                RegisterXdataApp(XDATA_REGAPP_NAME);

                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\nEntity 선택 (여러 개 - Line/Polyline/Block): ";

                // Line, Polyline, BlockReference 필터
                SelectionFilter filter = new SelectionFilter(
                    new TypedValue[] {
                        new TypedValue((int)DxfCode.Operator, "<OR"),
                        new TypedValue((int)DxfCode.Start, "LINE"),
                        new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                        new TypedValue((int)DxfCode.Start, "INSERT"),
                        new TypedValue((int)DxfCode.Operator, "OR>")
                    }
                );

                PromptSelectionResult psr = ed.GetSelection(pso, filter);
                if (psr.Status != PromptStatus.OK)
                    return;

                ed.WriteMessage("\n처리 중...");
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

                int lineCount = 0;
                int polylineCount = 0;
                int blockCount = 0;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        Entity entity = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                        if (entity != null)
                        {
                            ResultBuffer rb = new ResultBuffer(
                                new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDATA_REGAPP_NAME),
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString, "EntityDisplay")
                            );
                            entity.XData = rb;
                            rb.Dispose();

                            if (entity is Line)
                                lineCount++;
                            else if (entity is Polyline)
                                polylineCount++;
                            else if (entity is BlockReference)
                                blockCount++;
                        }
                    }

                    tr.Commit();
                }

                sw.Stop();
                ed.WriteMessage($"\n완료! '{XDATA_REGAPP_NAME}' Xdata 추가 ({sw.ElapsedMilliseconds}ms)");
                ed.WriteMessage($"\n  - Line: {lineCount}개");
                ed.WriteMessage($"\n  - Polyline: {polylineCount}개");
                ed.WriteMessage($"\n  - Block: {blockCount}개 (Cyan, LineWeight 30)");

                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }

        [CommandMethod("REMOVEARCHXDATA")]
        public void RemoveArchXdata()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                PromptEntityOptions peo = new PromptEntityOptions("\nEntity 선택: ");
                peo.SetRejectMessage("\nLine, Polyline, Block만 선택할 수 있습니다.");
                peo.AddAllowedClass(typeof(Line), true);
                peo.AddAllowedClass(typeof(Polyline), true);
                peo.AddAllowedClass(typeof(BlockReference), true);

                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity entity = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Entity;
                    if (entity != null)
                    {
                        ResultBuffer xdata = entity.GetXDataForApplication(XDATA_REGAPP_NAME);
                        if (xdata != null)
                        {
                            xdata.Dispose();
                            entity.XData = new ResultBuffer(
                                new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDATA_REGAPP_NAME)
                            );

                            ed.WriteMessage($"\n'{XDATA_REGAPP_NAME}' Xdata가 제거되었습니다.");
                            ed.WriteMessage("\n이제 이 Entity는 원래 상태로 표시됩니다!");
                        }
                        else
                        {
                            ed.WriteMessage($"\n'{XDATA_REGAPP_NAME}' Xdata가 없습니다.");
                        }
                    }

                    tr.Commit();
                }

                ed.Regen();
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }

        [CommandMethod("TESTXDATAFILTER")]
        public void TestXDataFilter()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                ed.WriteMessage("\n=== SetXDataFilter 테스트 (Multi-Entity) ===");

                int totalLines = 0, linesWithXdata = 0;
                int totalPolylines = 0, polylinesWithXdata = 0;
                int totalBlocks = 0, blocksWithXdata = 0;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId objId in btr)
                    {
                        Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                        if (entity == null)
                            continue;

                        ResultBuffer xdata = entity.GetXDataForApplication(XDATA_REGAPP_NAME);
                        bool hasXdata = xdata != null;
                        if (hasXdata)
                            xdata.Dispose();

                        if (entity is Line)
                        {
                            totalLines++;
                            if (hasXdata) linesWithXdata++;
                        }
                        else if (entity is Polyline)
                        {
                            totalPolylines++;
                            if (hasXdata) polylinesWithXdata++;
                        }
                        else if (entity is BlockReference)
                        {
                            totalBlocks++;
                            if (hasXdata) blocksWithXdata++;
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n\n[Line]");
                ed.WriteMessage($"\n  총 개수: {totalLines}");
                ed.WriteMessage($"\n  Xdata 있음: {linesWithXdata} (Red 색상)");

                ed.WriteMessage($"\n\n[Polyline]");
                ed.WriteMessage($"\n  총 개수: {totalPolylines}");
                ed.WriteMessage($"\n  Xdata 있음: {polylinesWithXdata} (Red 색상)");

                ed.WriteMessage($"\n\n[Block]");
                ed.WriteMessage($"\n  총 개수: {totalBlocks}");
                ed.WriteMessage($"\n  Xdata 있음: {blocksWithXdata} (회전 고려 Extents 박스 - Cyan, LW30)");

                int totalEntities = totalLines + totalPolylines + totalBlocks;
                int totalWithXdata = linesWithXdata + polylinesWithXdata + blocksWithXdata;

                if (totalEntities > 0)
                {
                    ed.WriteMessage($"\n\n→ WorldDraw 호출: {totalWithXdata}회 / {totalEntities}회");
                    ed.WriteMessage($"\n→ 절약률: {(100.0 * (totalEntities - totalWithXdata) / totalEntities):F1}%");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류: {ex.Message}");
            }
        }

        private void RegisterXdataApp(string appName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                RegAppTable rat = tr.GetObject(db.RegAppTableId, OpenMode.ForRead) as RegAppTable;
                if (!rat.Has(appName))
                {
                    rat.UpgradeOpen();
                    RegAppTableRecord ratr = new RegAppTableRecord();
                    ratr.Name = appName;
                    rat.Add(ratr);
                    tr.AddNewlyCreatedDBObject(ratr, true);
                }
                tr.Commit();
            }
        }
    }
}
