using System.Collections.Generic;
using AcadFunction;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    public class ChangeXdataNameCommand
    {
        [CommandMethod("ChangeXdataName", CommandFlags.UsePickSet)]
        public void Cmd_ChangeXdataName()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var db = doc.Database;
            var ed = doc.Editor;

            var psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK) return;
            var ids = psr.Value.GetObjectIds();
            if (ids.Length == 0) return;

            var oldOpt = new PromptStringOptions("\n변경할 기존 XData RegName: ") { AllowSpaces = false };
            var oldRes = ed.GetString(oldOpt);
            if (oldRes.Status != PromptStatus.OK) return;
            string oldName = oldRes.StringResult.Trim();
            if (string.IsNullOrEmpty(oldName))
            {
                ed.WriteMessage("\n기존 RegName이 비어있습니다.");
                return;
            }

            var newOpt = new PromptStringOptions("\n새 XData RegName: ") { AllowSpaces = false };
            var newRes = ed.GetString(newOpt);
            if (newRes.Status != PromptStatus.OK) return;
            string newName = newRes.StringResult.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                ed.WriteMessage("\n새 RegName이 비어있습니다.");
                return;
            }

            if (oldName == newName)
            {
                ed.WriteMessage("\n기존/새 RegName이 동일합니다.");
                return;
            }

            int changed = 0, skipped = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                JXdata.CheckXdataRegName(tr, newName);

                foreach (var id in ids)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) { skipped++; continue; }

                    var rbOld = ent.GetXDataForApplication(oldName);
                    if (rbOld == null) { skipped++; continue; }

                    var newValues = new List<TypedValue>();
                    foreach (TypedValue tv in rbOld)
                    {
                        if (tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName)
                            newValues.Add(new TypedValue(tv.TypeCode, newName));
                        else
                            newValues.Add(tv);
                    }
                    rbOld.Dispose();

                    ent.UpgradeOpen();
                    ent.XData = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, oldName));
                    using (var rbNew = new ResultBuffer(newValues.ToArray()))
                    {
                        ent.XData = rbNew;
                    }
                    changed++;
                }

                tr.Commit();
            }

            ed.WriteMessage($"\nChangeXdataName 완료: 변경 {changed}건, 건너뜀 {skipped}건 (RegName '{oldName}' → '{newName}')");
        }
    }
}
