using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch
{
    /// <summary>
    /// 라이선스 관련 명령.
    ///
    ///   JARCHID       이 컴퓨터의 고유 ID 를 명령창에 출력한다.
    ///   JARCLICENSE   등록창을 띄운다. 이미 등록된 PC 는 추가 등록되지 않는다.
    ///
    /// 판정과 등록은 전부 서버(Supabase)가 한다. 여기서는 값을 나르기만 한다.
    /// </summary>
    public class LicenseCommands
    {
        [CommandMethod("JARCHID")]
        public void ShowMachineId()
        {
            Editor? ed = AcApp.DocumentManager.MdiActiveDocument?.Editor;
            if (ed == null)
                return;

            string? id = JArch.Xdata.GetMachineId();
            if (string.IsNullOrEmpty(id))
            {
                ed.WriteMessage("\n[JArch] 컴퓨터 ID 를 확인하지 못했습니다.");
                return;
            }

            ed.WriteMessage($"\n[JArch] 이 컴퓨터의 ID : {id}");
            ed.WriteMessage("\n        등록은 JARCLICENSE 명령을 사용하세요.");

            // 전화·메일로 불러 주기 쉽게 클립보드에도 넣어 준다.
            try { Clipboard.SetText(id); ed.WriteMessage("\n        (클립보드에 복사됨)"); }
            catch { /* 클립보드 실패는 무시 */ }
        }

        [CommandMethod("JARCLICENSE")]
        public void RegisterLicense()
        {
            Editor? ed = AcApp.DocumentManager.MdiActiveDocument?.Editor;
            if (ed == null)
                return;

            string? id = JArch.Xdata.GetMachineId();
            if (string.IsNullOrEmpty(id))
            {
                ed.WriteMessage("\n[JArch] 컴퓨터 ID 를 확인하지 못해 등록할 수 없습니다.");
                return;
            }

            // 이미 등록돼 있으면 창을 띄우지 않는다. 서버도 PK 로 막지만,
            // 사용자가 입력을 다 하고 나서 거절당하지 않게 여기서 먼저 걸러 준다.
            JArch.Xdata.LicenseInfo lic = JArch.Xdata.GetInfo();
            switch (lic.Status)
            {
                case JArch.Xdata.LicenseStatus.Valid:
                    ed.WriteMessage($"\n[JArch] 이미 등록된 컴퓨터입니다. (사용기한 {lic.EndDate:yyyy-MM-dd} 까지)");
                    return;

                case JArch.Xdata.LicenseStatus.Expired:
                    ed.WriteMessage($"\n[JArch] 이미 등록된 컴퓨터이며 사용기한이 지났습니다. (만료일 {lic.EndDate:yyyy-MM-dd})");
                    ed.WriteMessage("\n        기간 연장은 관리자에게 문의하세요. 재등록은 되지 않습니다.");
                    return;

                case JArch.Xdata.LicenseStatus.Unreachable:
                    ed.WriteMessage("\n[JArch] 라이선스 서버에 연결할 수 없어 등록할 수 없습니다.");
                    return;
            }

            using var form = new LicenseRegisterForm(id);
            if (AcApp.ShowModalDialog(form) != DialogResult.OK)
            {
                ed.WriteMessage("\n[JArch] 등록이 취소되었습니다.");
                return;
            }

            JArch.Xdata.RegisterResult r = JArch.Xdata.Register(
                form.UserName, form.CompName, form.PartName, out string expDate);

            switch (r)
            {
                case JArch.Xdata.RegisterResult.Registered:
                    ed.WriteMessage($"\n[JArch] 등록되었습니다. 사용기한 {expDate} 까지.");
                    ed.WriteMessage("\n        AutoCAD 를 다시 시작하면 적용됩니다.");
                    MyPlugin.ResetLicenseCache();
                    break;

                case JArch.Xdata.RegisterResult.AlreadyExists:
                    ed.WriteMessage("\n[JArch] 이미 등록된 컴퓨터입니다. 추가 등록은 되지 않습니다.");
                    break;

                case JArch.Xdata.RegisterResult.BadId:
                    ed.WriteMessage("\n[JArch] 컴퓨터 ID 형식이 올바르지 않아 등록하지 못했습니다.");
                    break;

                default:
                    ed.WriteMessage("\n[JArch] 서버에 연결하지 못해 등록하지 못했습니다.");
                    break;
            }
        }
    }

    /// <summary>
    /// 라이선스 등록 입력창. 입력만 받고 통신은 호출자가 한다.
    ///
    /// Designer 파일을 두지 않고 생성자에서 배치한다 - 이 프로젝트에서 VS Designer 가
    /// 코드로 추가한 컨트롤 설정을 지워 버린 전례가 있어, 작은 입력창은 코드로만 만든다.
    /// </summary>
    internal sealed class LicenseRegisterForm : Form
    {
        private readonly TextBox _txtUser = new();
        private readonly TextBox _txtComp = new();
        private readonly TextBox _txtPart = new();
        private readonly Button  _btnOk   = new();

        public string UserName => _txtUser.Text.Trim();
        public string CompName => _txtComp.Text.Trim();
        public string PartName => _txtPart.Text.Trim();

        public LicenseRegisterForm(string machineId)
        {
            Text            = "JArchitecture 라이선스 등록";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            Font            = new Font("맑은 고딕", 9F);
            AutoScaleMode   = AutoScaleMode.Font;

            // 크기를 내용에서 정한다. 픽셀로 고정하면 AutoCAD 의 DPI 배율이나
            // 글꼴이 달라졌을 때 글자가 잘린다(실제로 잘렸다).
            AutoSize     = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // 입력칸 너비도 글꼴로 재서 정한다 — 배율이 커져도 ID 가 한 줄에 들어간다.
            int boxWidth = TextRenderer.MeasureText(machineId + "12345678", Font).Width;

            var grid = new TableLayoutPanel
            {
                ColumnCount  = 2,
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding      = new Padding(16, 14, 16, 12),
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var txtId = new TextBox
            {
                Text        = machineId,
                ReadOnly    = true,
                TabStop     = false,
                BorderStyle = BorderStyle.None,
                BackColor   = SystemColors.Control,
                Width       = boxWidth,
                Margin      = new Padding(3, 7, 3, 9),
            };

            AddRow(grid, "컴퓨터 ID", txtId);
            AddRow(grid, "사용자 *",  SetUp(_txtUser, boxWidth));
            AddRow(grid, "회사명",    SetUp(_txtComp, boxWidth));
            AddRow(grid, "부서명",    SetUp(_txtPart, boxWidth));

            _txtUser.TextChanged += (s, e) => _btnOk.Enabled = UserName.Length > 0;

            // 줄바꿈 위치를 직접 정한다. 자동 줄바꿈에 맡기면 배율에 따라 갈라지는 곳이 달라진다.
            var hint = new Label
            {
                Text      = "등록 후 사용기한은 서버가 부여합니다." + Environment.NewLine +
                            "한 컴퓨터는 한 번만 등록됩니다.",
                AutoSize  = true,
                ForeColor = SystemColors.GrayText,
                Margin    = new Padding(0, 10, 0, 6),
            };
            grid.Controls.Add(hint);
            grid.SetColumnSpan(hint, 2);

            _btnOk.Text          = "등록";
            _btnOk.DialogResult  = DialogResult.OK;
            _btnOk.Enabled       = false;          // 사용자명을 넣어야 열린다
            _btnOk.AutoSize      = true;
            _btnOk.Padding       = new Padding(16, 4, 16, 4);
            _btnOk.Margin        = new Padding(8, 0, 0, 0);

            var btnCancel = new Button
            {
                Text         = "취소",
                DialogResult = DialogResult.Cancel,
                AutoSize     = true,
                Padding      = new Padding(16, 4, 16, 4),
                Margin       = new Padding(8, 0, 0, 0),
            };

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Anchor        = AnchorStyles.Right,
                Margin        = new Padding(0, 6, 0, 0),
            };
            buttons.Controls.Add(btnCancel);      // RightToLeft 라 취소가 오른쪽 끝
            buttons.Controls.Add(_btnOk);
            grid.Controls.Add(buttons);
            grid.SetColumnSpan(buttons, 2);

            Controls.Add(grid);

            AcceptButton  = _btnOk;
            CancelButton  = btnCancel;
            ActiveControl = _txtUser;
        }

        private static TextBox SetUp(TextBox box, int width)
        {
            box.Width     = width;
            box.MaxLength = 50;
            box.Margin    = new Padding(3, 3, 3, 6);
            return box;
        }

        private static void AddRow(TableLayoutPanel grid, string caption, Control field)
        {
            grid.Controls.Add(new Label
            {
                Text     = caption,
                AutoSize = true,
                Anchor   = AnchorStyles.Left,
                Margin   = new Padding(0, 7, 14, 6),
            });
            grid.Controls.Add(field);
        }
    }
}
