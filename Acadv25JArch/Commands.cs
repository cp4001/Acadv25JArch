using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CADExtension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Color = Autodesk.AutoCAD.Colors.Color;
using FlowDirection = System.Windows.Forms.FlowDirection;
using Image = System.Drawing.Image;


namespace Acadv25JArch
{
    public class Commands
    {

        [CommandMethod(Jdf.Cmd.ShowBlockForm)]
        public void Cmd_ShowBlockForm()
        {
            FormBlockPart  form = new FormBlockPart();
            //Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(form);
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(form);
            //form.ShowDialog();
        }

        [CommandMethod("ANGLE2LINE")]
        public void Cmd_ShowAngleBetweenTwoLines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 첫 번째 선 선택
                    PromptEntityOptions peo1 = new PromptEntityOptions("\n첫 번째 선을 선택하세요: ");
                    peo1.SetRejectMessage("\n선(Line)만 선택 가능합니다.");
                    peo1.AddAllowedClass(typeof(Line), true);

                    PromptEntityResult per1 = ed.GetEntity(peo1);
                    if (per1.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n선택이 취소되었습니다.");
                        return;
                    }

                    // 두 번째 선 선택
                    PromptEntityOptions peo2 = new PromptEntityOptions("\n두 번째 선을 선택하세요: ");
                    peo2.SetRejectMessage("\n선(Line)만 선택 가능합니다.");
                    peo2.AddAllowedClass(typeof(Line), true);

                    PromptEntityResult per2 = ed.GetEntity(peo2);
                    if (per2.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n선택이 취소되었습니다.");
                        return;
                    }

                    // 선 객체 가져오기
                    Line line1 = tr.GetObject(per1.ObjectId, OpenMode.ForRead) as Line;
                    Line line2 = tr.GetObject(per2.ObjectId, OpenMode.ForRead) as Line;

                    if (line1 == null || line2 == null)
                    {
                        ed.WriteMessage("\n선을 가져올 수 없습니다.");
                        return;
                    }

                    // 내각 계산
                    double angle = line1.GetAngle(line2);
                    //double anglePlus180 = angle + 180;

                    // 결과 출력
                    ed.WriteMessage($"\n========================================");
                    ed.WriteMessage($"\n두 선 사이의 내각: {angle:F2}°");
                    //ed.WriteMessage($"\n내각 + 180°: {anglePlus180:F2}°");
                    ed.WriteMessage($"\n========================================");

                    //// 선 정보도 출력
                    //ed.WriteMessage($"\n\n[선 1 정보]");
                    //ed.WriteMessage($"\n  시작점: ({line1.StartPoint.X:F2}, {line1.StartPoint.Y:F2})");
                    //ed.WriteMessage($"\n  끝점: ({line1.EndPoint.X:F2}, {line1.EndPoint.Y:F2})");
                    //ed.WriteMessage($"\n  길이: {line1.Length:F2}");

                    //ed.WriteMessage($"\n\n[선 2 정보]");
                    //ed.WriteMessage($"\n  시작점: ({line2.StartPoint.X:F2}, {line2.StartPoint.Y:F2})");
                    //ed.WriteMessage($"\n  끝점: ({line2.EndPoint.X:F2}, {line2.EndPoint.Y:F2})");
                    //ed.WriteMessage($"\n  길이: {line2.Length:F2}\n");

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n오류 발생: {ex.Message}");
            }
        }


        [CommandMethod("QQ")]
        public void SelectSimilarShortcut()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            // SELECTSIMILAR 명령어 실행
            doc.SendStringToExecute("_SELECTSIMILAR ", true, false, false);
        }


    }
}
