using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCADLineLengthOverrule
{
    /// <summary>
    /// SetXDataFilter를 사용하여 AutoCAD 내부에서 필터링
    /// Xdata가 없는 객체는 WorldDraw가 아예 호출되지 않음!
    /// </summary>
    public class XDataFilterDrawOverrule : DrawableOverrule
    {
        private static XDataFilterDrawOverrule _instance;
        private const string XDATA_REGAPP_NAME = "Arch";

        public static void Register()
        {
            if (_instance == null)
            {
                _instance = new XDataFilterDrawOverrule();

                // Line RXClass 가져오기
                RXClass lineClass = RXObject.GetClass(typeof(Line));

                // Overrule 추가
                Overrule.AddOverrule(lineClass, _instance, false);

                // 핵심: SetXDataFilter 설정 (인스턴스 메서드로 호출!)
                _instance.SetXDataFilter(XDATA_REGAPP_NAME);

                // Overruling 활성화
                Overrule.Overruling = true;

                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\n========================================");
                    doc.Editor.WriteMessage("\n[XDataFilter] Overrule 등록 완료");
                    doc.Editor.WriteMessage("\n========================================");
                    doc.Editor.WriteMessage($"\n✓ AutoCAD 내부에서 '{XDATA_REGAPP_NAME}' Xdata 필터링");
                    doc.Editor.WriteMessage("\n✓ Xdata 없는 Line은 WorldDraw 호출 안됨");
                    doc.Editor.WriteMessage("\n✓ IsApplicable도 Xdata 있는 객체만 호출");
                    doc.Editor.WriteMessage("\n✓ 최소한의 오버헤드로 최고 성능!");
                    doc.Editor.WriteMessage("\n========================================");
                }
            }
        }

        public static void Unregister()
        {
            if (_instance != null)
            {
                RXClass lineClass = RXObject.GetClass(typeof(Line));

                // Overrule 제거
                Overrule.RemoveOverrule(lineClass, _instance);

                _instance = null;

                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\n[XDataFilter] Overrule 제거 완료");
                }
            }
        }

        /// <summary>
        /// IsApplicable - SetXDataFilter 덕분에 Xdata가 있는 객체만 호출됨
        /// </summary>
        public override bool IsApplicable(RXObject overruledSubject)
        {
            // SetXDataFilter가 이미 필터링했으므로
            // 여기 도달한 객체는 모두 Xdata가 있는 객체임

            Line line = overruledSubject as Line;
            if (line == null)
                return false;

            if (line.ObjectId.IsNull || line.ObjectId.IsErased)
                return false;

            // 추가 필터링이 필요하면 여기서 (예: Layer, Color 등)
            return true;
        }

        /// <summary>
        /// WorldDraw - Xdata가 있는 Line만 호출됨!
        /// </summary>
        public override bool WorldDraw(Drawable drawable, WorldDraw wd)
        {
            // 기본 Line 그리기
            bool result = base.WorldDraw(drawable, wd);

            Line line = drawable as Line;
            if (line == null)
                return result;

            try
            {
                // Line 데이터 계산
                double length = line.Length;
                string lengthText = length.ToString("F2");

                Point3d centerPoint = new Point3d(
                    (line.StartPoint.X + line.EndPoint.X) / 2,
                    (line.StartPoint.Y + line.EndPoint.Y) / 2,
                    (line.StartPoint.Z + line.EndPoint.Z) / 2
                );

                Vector3d direction = (line.EndPoint - line.StartPoint).GetNormal();

                // 수직 방향 계산
                Vector3d perpendicular = new Vector3d(-direction.Y, direction.X, 0);
                if (perpendicular.Length > 0.0001)
                    perpendicular = perpendicular.GetNormal();
                else
                    perpendicular = Vector3d.YAxis;

                // 텍스트 높이 (고정값)
                double textHeight = 2.5;

                // 텍스트 위치 (Line 중심에서 약간 위로)
                Point3d textPosition = centerPoint + perpendicular * textHeight * 0.5;

                // 텍스트 각도 계산
                double angle = Math.Atan2(direction.Y, direction.X);
                if (angle > Math.PI / 2 || angle < -Math.PI / 2)
                    angle += Math.PI;

                // 임시 MText 생성하여 그리기 (가장 안전한 방법)
                using (MText mtext = new MText())
                {
                    mtext.SetDatabaseDefaults();
                    mtext.Location = textPosition;
                    mtext.TextHeight = textHeight;
                    mtext.Contents = lengthText;
                    mtext.Rotation = angle;
                    mtext.Attachment = AttachmentPoint.MiddleCenter;

                    // MText를 WorldDraw로 그리기
                    mtext.WorldDraw(wd);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WorldDraw Error: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// 명령어 클래스
    /// </summary>
    public class XDataFilterCommands
    {
        private const string XDATA_REGAPP_NAME = "Arch";

        [CommandMethod("REGISTERXDATAFILTER")]
        public void RegisterXDataFilter()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                RegisterXdataApp(XDATA_REGAPP_NAME);

                XDataFilterDrawOverrule.Register();

                ed.WriteMessage("\n[최적 성능] SetXDataFilter 방식 활성화!");

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

                PromptEntityOptions peo = new PromptEntityOptions("\nLine 선택: ");
                peo.SetRejectMessage("\nLine만 선택할 수 있습니다.");
                peo.AddAllowedClass(typeof(Line), true);

                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Line line = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Line;
                    if (line != null)
                    {
                        ResultBuffer rb = new ResultBuffer(
                            new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDATA_REGAPP_NAME),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, "LineLengthDisplay")
                        );
                        line.XData = rb;
                        rb.Dispose();

                        ed.WriteMessage($"\n'{XDATA_REGAPP_NAME}' Xdata가 추가되었습니다.");
                        ed.WriteMessage("\n이제 이 Line은 SetXDataFilter를 통과합니다!");
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
                pso.MessageForAdding = "\nLine 선택 (여러 개): ";

                SelectionFilter filter = new SelectionFilter(
                    new TypedValue[] { new TypedValue((int)DxfCode.Start, "LINE") }
                );

                PromptSelectionResult psr = ed.GetSelection(pso, filter);
                if (psr.Status != PromptStatus.OK)
                    return;

                ed.WriteMessage("\n처리 중...");
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

                int count = 0;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in psr.Value)
                    {
                        Line line = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Line;
                        if (line != null)
                        {
                            ResultBuffer rb = new ResultBuffer(
                                new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDATA_REGAPP_NAME),
                                new TypedValue((int)DxfCode.ExtendedDataAsciiString, "LineLengthDisplay")
                            );
                            line.XData = rb;
                            rb.Dispose();
                            count++;
                        }
                    }

                    tr.Commit();
                }

                sw.Stop();
                ed.WriteMessage($"\n완료! {count}개의 Line에 '{XDATA_REGAPP_NAME}' Xdata 추가 ({sw.ElapsedMilliseconds}ms)");
                ed.WriteMessage("\n이제 이 Line들은 SetXDataFilter를 통과합니다!");

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
                PromptEntityOptions peo = new PromptEntityOptions("\nLine 선택: ");
                peo.SetRejectMessage("\nLine만 선택할 수 있습니다.");
                peo.AddAllowedClass(typeof(Line), true);

                PromptEntityResult per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                    return;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Line line = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Line;
                    if (line != null)
                    {
                        ResultBuffer xdata = line.GetXDataForApplication(XDATA_REGAPP_NAME);
                        if (xdata != null)
                        {
                            xdata.Dispose();
                            line.XData = new ResultBuffer(
                                new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDATA_REGAPP_NAME)
                            );

                            ed.WriteMessage($"\n'{XDATA_REGAPP_NAME}' Xdata가 제거되었습니다.");
                            ed.WriteMessage("\n이제 이 Line은 SetXDataFilter에 걸러집니다!");
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
                ed.WriteMessage("\n=== SetXDataFilter 테스트 ===");

                int totalLines = 0;
                int linesWithXdata = 0;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId objId in btr)
                    {
                        if (objId.ObjectClass.DxfName == "LINE")
                        {
                            totalLines++;
                            Line line = tr.GetObject(objId, OpenMode.ForRead) as Line;
                            if (line != null)
                            {
                                ResultBuffer xdata = line.GetXDataForApplication(XDATA_REGAPP_NAME);
                                if (xdata != null)
                                {
                                    linesWithXdata++;
                                    xdata.Dispose();
                                }
                            }
                        }
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\n\n총 Line 개수: {totalLines}");
                ed.WriteMessage($"\nXdata '{XDATA_REGAPP_NAME}' 있는 Line: {linesWithXdata}");
                ed.WriteMessage($"\n필터링되는 Line: {totalLines - linesWithXdata}");

                if (totalLines > 0)
                {
                    ed.WriteMessage($"\n\n→ WorldDraw 호출 횟수: {linesWithXdata}회만! (전체의 {(100.0 * linesWithXdata / totalLines):F1}%)");
                    ed.WriteMessage($"\n→ 절약되는 WorldDraw 호출: {totalLines - linesWithXdata}회 ({(100.0 * (totalLines - linesWithXdata) / totalLines):F1}%)");
                }

                if (totalLines - linesWithXdata > 0)
                {
                    ed.WriteMessage("\n\n✓ SetXDataFilter가 효과적으로 작동 중!");
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
