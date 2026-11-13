using AcadFunction;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using CADExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PipeLoad2
{
    public partial class RoomEquipForm : Form
    {
        private readonly Document _doc;
        private readonly List<RoomAACommand.RoomRow> _rooms;

        /// <summary>Spec Text 줄 간격 = 글자 높이 × 이 값.</summary>
        private const double SpecLineGap = 1.8;

        public RoomEquipForm(Document doc, List<RoomAACommand.RoomRow> rows)
        {
            InitializeComponent();
            _doc = doc;
            _rooms = rows;
            foreach (var r in rows)
            {
                int i = dgvRoom.Rows.Add(r.No, r.Name, r.Heat, r.Cool, r.RoomQ, r.Etc);
                dgvRoom.Rows[i].Tag = r.Handle;
            }

            // 장비 탭 콤보 — 실 목록(이름 중복·빈칸 대비 번호 표시), Type·SystemType 은 Diffuser_Spec 과 같은 목록
            foreach (var r in rows) cmbEqRoom.Items.Add($"{r.No}. {r.Name}");
            cmbEqType.Items.AddRange(DiffuserInsertCommand.Types);
            cmbEqSys.Items.AddRange(DiffuserInsertCommand.SystemTypes);
            if (cmbEqRoom.Items.Count > 0) cmbEqRoom.SelectedIndex = 0;
            cmbEqType.SelectedIndex = 0;
            cmbEqSys.SelectedIndex = 0;
            LoadEquip();
        }

        /// <summary>폼 열 때 각 실 Poly 안 기존 Spec Text(XData Diffuser) → dgvEquip (실 순서, 위→아래).</summary>
        private void LoadEquip()
        {
            var sel = _doc.Editor.SelectAll(JSelFilter.MakeFilterTypesRegs("TEXT", "Diffuser"));
            if (sel.Status != PromptStatus.OK) return;
            Database db = _doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var texts = sel.Value.GetObjectIds()
                    .Select(id => tr.GetObject(id, OpenMode.ForRead) as DBText)
                    .Where(t => t != null)
                    .Select(t => t!)
                    .ToList();
                foreach (var room in _rooms)
                {
                    if (!db.TryGetObjectId(new Handle(Convert.ToInt64(room.Handle, 16)), out ObjectId plId) ||
                        plId.IsErased ||
                        tr.GetObject(plId, OpenMode.ForRead) is not Polyline pl)
                        continue;
                    foreach (var t in texts.Where(t => DiffuserInsertCommand.IsInsidePoly(pl, t.Position))
                                           .OrderByDescending(t => t.Position.Y))
                    {
                        if (!DiffuserInsertCommand.TryParseSpec(t.TextString, out var sp, out _)) continue;
                        dgvEquip.Rows.Add(room.Name, txtEqKind.Text, sp!.Type, sp.SystemType, sp.Cfm, sp.Count);
                    }
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// +ADD — "CFM,Type,SystemType,개수" Spec Text 를 해당 실 Poly 안 RoomText 아래(기존 Spec Text 가 있으면 그 아래)에
        /// 생성하고 Diffuser_Spec 과 같은 표시(ACI 41, 기울기 5°) + XData Diffuser=Type 기록 → 그리드에 행 추가.
        /// </summary>
        private void btnEqAdd_Click(object? sender, EventArgs e)
        {
            if (cmbEqRoom.SelectedIndex < 0)
            {
                MessageBox.Show(this, "실을 선택하세요.", "실 / 장비", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var room = _rooms[cmbEqRoom.SelectedIndex];
            string spec = $"{txtEqCfm.Text.Trim()},{cmbEqType.Text},{cmbEqSys.Text},{numEqCount.Value}";
            if (!DiffuserInsertCommand.TryParseSpec(spec, out var sp, out string reason))
            {
                MessageBox.Show(this, $"입력 오류: {reason}", "실 / 장비", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? err;
            try { err = AddSpecText(room.Handle, spec, sp!.Type); }
            catch (Exception ex) { err = ex.Message; }
            if (err != null)
            {
                MessageBox.Show(this, $"추가 실패: {err}", "실 / 장비", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            dgvEquip.Rows.Add(room.Name, txtEqKind.Text, sp.Type, sp.SystemType, txtEqCfm.Text.Trim(), sp.Count);
        }

        /// <summary>Spec Text 생성. 성공 시 null, 실패 시 사유.</summary>
        private string? AddSpecText(string handle, string spec, string type)
        {
            Database db = _doc.Database;
            Editor ed = _doc.Editor;
            using (_doc.LockDocument())
            {
                // RoomText / 기존 Spec Text 수집 (Editor 호출 → Transaction 밖)
                ObjectId[] Select(string reg)
                {
                    var r = ed.SelectAll(JSelFilter.MakeFilterTypesRegs("TEXT", reg));
                    return r.Status == PromptStatus.OK ? r.Value.GetObjectIds() : Array.Empty<ObjectId>();
                }
                ObjectId[] roomTextIds = Select("RoomText");
                ObjectId[] specIds = Select("Diffuser");

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    if (!db.TryGetObjectId(new Handle(Convert.ToInt64(handle, 16)), out ObjectId plId) ||
                        plId.IsErased ||
                        tr.GetObject(plId, OpenMode.ForRead) is not Polyline pl)
                        return "실 Poly 를 찾을 수 없습니다 (삭제됨).";

                    List<DBText> Inside(ObjectId[] ids) => ids
                        .Select(id => tr.GetObject(id, OpenMode.ForRead) as DBText)
                        .Where(t => t != null && DiffuserInsertCommand.IsInsidePoly(pl, t.Position))
                        .Select(t => t!)
                        .ToList();
                    DBText? roomText = Inside(roomTextIds).OrderByDescending(t => t.Position.Y).FirstOrDefault();
                    var stack = Inside(specIds);
                    if (roomText != null) stack.Add(roomText);
                    DBText? lowest = stack.OrderBy(t => t.Position.Y).FirstOrDefault();

                    double h = roomText?.Height ?? lowest?.Height ?? db.Textsize;
                    Point3d pos;
                    if (lowest != null)
                        pos = new Point3d((roomText ?? lowest).Position.X, lowest.Position.Y - h * SpecLineGap, 0);
                    else
                    {
                        Extents3d ext = pl.GeometricExtents;
                        pos = new Point3d((ext.MinPoint.X + ext.MaxPoint.X) / 2, (ext.MinPoint.Y + ext.MaxPoint.Y) / 2, 0);
                    }

                    var txt = new DBText
                    {
                        Position = pos,
                        Height = h,
                        TextString = spec,
                        TextStyleId = roomText?.TextStyleId ?? db.Textstyle,
                        ColorIndex = DiffuserInsertCommand.SpecColorIndex,
                        Oblique = DiffuserInsertCommand.SpecObliqueDeg * Math.PI / 180.0,
                    };
                    var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    space.AppendEntity(txt);
                    tr.AddNewlyCreatedDBObject(txt, true);

                    tr.ChecRegNames(db, "Diffuser");
                    JXdata.SetXdata(txt, "Diffuser", type);
                    // arx 는 라이선스 차단 시 기록 없이 성공 반환 → 되읽어 확인
                    if (JXdata.GetXdata(txt, "Diffuser") != type)
                        return "XData Diffuser 가 기록되지 않았습니다. 라이선스 상태를 확인하세요 (JARCLICENSE).";

                    tr.TransactionManager.QueueForGraphicsFlush();
                    tr.Commit();
                }
                // 모덜리스 폼 → 도면 클릭 전까지 화면 갱신 안 됨. 즉시 다시 그리기
                _doc.TransactionManager.FlushGraphics();
                ed.UpdateScreen();
            }
            return null;
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
