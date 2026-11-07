using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PipeLoad2
{
    /// <summary>
    /// RoomAA — Room Poly(XData "Room") 다중 선택 → 실/장비 탭 Form.
    /// 실 탭: Room No(선택 순번) / 실명(Poly 안 RoomText) / HeatLoad / CoolLoad / RoomQ / Etc.
    /// 4개 XData 중 없는 것은 " " 로 생성한다.
    /// </summary>
    public class RoomAACommand
    {
        public record RoomRow(int No, string Name, string Heat, string Cool, string RoomQ, string Etc, string Handle);

        internal static readonly string[] LoadKeys = { "HeatLoad", "CoolLoad", "RoomQ", "Etc" };

        [CommandMethod("RoomAA", CommandFlags.UsePickSet)]
        public void Cmd_RoomAA()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // RoomText 수집 (Editor 호출 → Transaction 밖)
            PromptSelectionResult rsr = ed.SelectAll(JSelFilter.MakeFilterTypesRegs("TEXT", "RoomText"));
            ObjectId[] roomTextIds = rsr.Status == PromptStatus.OK ? rsr.Value.GetObjectIds() : Array.Empty<ObjectId>();

            var rows = new List<RoomRow>();
            int created = 0, read = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Polyline> polys = JEntityFunc.GetEntityByTpye<Polyline>(
                    "Room Poly 를 선택하세요",
                    JSelFilter.MakeFilterTypesRegs("LWPOLYLINE", "Room"));
                if (polys == null || polys.Count == 0) return;

                tr.ChecRegNames(db, string.Join(",", LoadKeys));

                var texts = roomTextIds
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as DBText)
                    .Where(t => t != null)
                    .ToList();

                int no = 1;
                foreach (Polyline pl in polys)
                {
                    string name = texts
                        .Where(t => DiffuserInsertCommand.IsInsidePoly(pl, t!.Position))
                        .OrderByDescending(t => t!.Position.Y)
                        .Select(t => t!.TextString)
                        .FirstOrDefault() ?? "";
                    if (name.Length == 0)
                        ed.WriteMessage($"\n[알림] Poly(핸들 {pl.Handle}) 안에 RoomText 가 없습니다.");

                    // 없는 XData 는 " " 로 생성 (잠긴 레이어는 읽기만)
                    var layer = (LayerTableRecord)tr.GetObject(pl.LayerId, OpenMode.ForRead);
                    var vals = new string[LoadKeys.Length];
                    for (int i = 0; i < LoadKeys.Length; i++)
                    {
                        string? v = JXdata.GetXdata(pl, LoadKeys[i]);
                        if (v != null) read++;
                        else if (!layer.IsLocked)
                        {
                            if (!pl.IsWriteEnabled) pl.UpgradeOpen();
                            JXdata.SetXdata(pl, LoadKeys[i], " ");
                            created++;
                            v = " ";
                        }
                        vals[i] = v ?? "";
                    }

                    rows.Add(new RoomRow(no++, name, vals[0], vals[1], vals[2], vals[3], pl.Handle.ToString()));
                }
                tr.Commit();
            }

            ed.WriteMessage($"\n[RoomAA] Poly {rows.Count}개 — 기존 XData 읽음 {read}개, 새로 생성(\" \") {created}개.");
            Application.ShowModelessDialog(new RoomEquipForm(doc, rows));
        }
    }
}
