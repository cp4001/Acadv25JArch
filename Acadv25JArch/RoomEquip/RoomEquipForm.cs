using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CADExtension;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PipeLoad2
{
    public partial class RoomEquipForm : Form
    {
        private readonly Document _doc;

        public RoomEquipForm(Document doc, List<RoomAACommand.RoomRow> rows)
        {
            InitializeComponent();
            _doc = doc;
            foreach (var r in rows)
            {
                int i = dgvRoom.Rows.Add(r.No, r.Name, r.Heat, r.Cool, r.RoomQ, r.Etc);
                dgvRoom.Rows[i].Tag = r.Handle;
            }
        }

        /// <summary>그리드 4열(난방/냉방/총풍량/비고) → 각 Poly XData. 빈 칸은 " " 로 기록.</summary>
        private void btnSave_Click(object? sender, EventArgs e)
        {
            dgvRoom.EndEdit();
            Database db = _doc.Database;
            int saved = 0, skipped = 0, notWritten = 0;
            try
            {
                using (_doc.LockDocument())
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    tr.ChecRegNames(db, string.Join(",", RoomAACommand.LoadKeys));
                    foreach (DataGridViewRow row in dgvRoom.Rows)
                    {
                        if (row.Tag is not string h ||
                            !db.TryGetObjectId(new Handle(Convert.ToInt64(h, 16)), out ObjectId id) ||
                            id.IsErased ||
                            tr.GetObject(id, OpenMode.ForRead) is not Polyline pl)
                        { skipped++; continue; }

                        var layer = (LayerTableRecord)tr.GetObject(pl.LayerId, OpenMode.ForRead);
                        if (layer.IsLocked) { skipped++; continue; }

                        pl.UpgradeOpen();
                        for (int i = 0; i < RoomAACommand.LoadKeys.Length; i++)
                        {
                            string v = Convert.ToString(row.Cells[i + 2].Value)?.Trim() ?? "";
                            if (v.Length == 0) v = " ";
                            JXdata.SetXdata(pl, RoomAACommand.LoadKeys[i], v);
                            // arx 는 라이선스 차단 시 기록 없이 성공 반환 → 되읽어 확인
                            if (JXdata.GetXdata(pl, RoomAACommand.LoadKeys[i]) != v) notWritten++;
                        }
                        saved++;                    }
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"저장 실패: {ex.Message}", "실 / 장비", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (notWritten > 0)
            {
                MessageBox.Show(this, $"XData {notWritten}개가 기록되지 않았습니다.\n라이선스 상태를 확인하세요 (JARCLICENSE).",
                    "실 / 장비", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string msg = $"{saved}개 실 저장 완료." + (skipped > 0 ? $"\n{skipped}개 건너뜀 (삭제됨/잠긴 레이어)." : "");
            MessageBox.Show(this, msg, "실 / 장비", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
