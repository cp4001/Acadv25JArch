// =====================================================================
//  AutoCAD 2025 .NET API - PaletteSet Sample
//  탭 없이 도킹/플로팅 가능한 도구 팔레트로 명령 버튼 그룹 표시
//
//  명령:
//    SHOWPAL → 표시(없으면 생성), HIDEPAL → 숨김
//
//  주의:
//    - [assembly: ExtensionApplication] 은 MyPlugIn.cs 가 이미 점유.
//      여기서는 lazy init 패턴 사용 (LayerManagerPalette 와 동일).
// =====================================================================

using System;
using System.Drawing;
using System.Windows.Forms;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;            // PaletteSet  (AcMgd.dll)
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PaletteSample
{
    public class PaletteCommands
    {
        [CommandMethod("SHOWPAL")]
        public void ShowPalette()
        {
            PaletteHost.EnsureCreated();
            PaletteHost.Palette.Visible = true;
        }

        [CommandMethod("HIDEPAL")]
        public void HidePalette()
        {
            if (PaletteHost.Palette != null)
                PaletteHost.Palette.Visible = false;
        }
    }

    public static class PaletteHost
    {
        // GUID 는 한 번 정해두면 위치/크기 사용자 설정이 저장됨
        private static readonly Guid PaletteGuid =
            new Guid("F1B2C3D4-1111-2222-3333-AABBCCDDEEFF");

        public static PaletteSet Palette { get; private set; }

        public static void EnsureCreated()
        {
            if (Palette != null) return;

            Palette = new PaletteSet("My Tools", PaletteGuid)
            {
                Size  = new Size(520, 90),
                Style = PaletteSetStyles.ShowAutoHideButton
                      | PaletteSetStyles.ShowCloseButton
                      | PaletteSetStyles.Snappable,
                DockEnabled = DockSides.Left | DockSides.Right
                            | DockSides.Top  | DockSides.Bottom
            };

            Palette.Add("Tools", new MyToolsControl());
        }
    }

    public class MyToolsControl : UserControl
    {
        private readonly ToolTip _tip = new ToolTip
        {
            AutoPopDelay = 5000,
            InitialDelay = 400,
            ReshowDelay  = 200,
            ShowAlways   = true
        };

        public MyToolsControl()
        {
            Dock      = DockStyle.Fill;
            BackColor = Color.FromArgb(60, 60, 60);

            FlowLayoutPanel root = new FlowLayoutPanel
            {
                Dock           = DockStyle.Fill,
                FlowDirection  = System.Windows.Forms.FlowDirection.LeftToRight,
                WrapContents   = true,
                Padding        = new Padding(4),
                AutoScroll     = true
            };
            Controls.Add(root);

            root.Controls.Add(MakeBigButton("Fcal", "_LINE ",  "Fire Calculation"));

            root.Controls.Add(MakeGroup(new[]
            {
                ("To Fire", "_CIRCLE ",  "To Fire 명령"),
                ("To Main", "_RECTANG ", "To Main 명령")
            }));

            root.Controls.Add(MakeGroup(new[]
            {
                ("Pipe",  "_PLINE ", "Pipe 그리기"),
                ("Split", "_BREAK ", "객체 분할"),
                ("Merge", "_PEDIT ", "객체 병합")
            }));

            root.Controls.Add(MakeGroup(new[]
            {
                ("FD", "_ZOOM E ", "Fire Detector"),
                ("FP", "_ZOOM A ", "Fire Pump"),
                ("FH", "_ZOOM W ", "Fire Hydrant")
            }));
        }

        private Panel MakeGroup((string text, string cmd, string tip)[] items)
        {
            FlowLayoutPanel grp = new FlowLayoutPanel
            {
                FlowDirection = System.Windows.Forms.FlowDirection.TopDown,
                AutoSize      = true,
                WrapContents  = false,
                Margin        = new Padding(4, 2, 4, 2),
                Padding       = new Padding(3),
                BorderStyle   = BorderStyle.FixedSingle,
                BackColor     = Color.FromArgb(75, 75, 75)
            };
            foreach (var it in items)
                grp.Controls.Add(MakeSmallButton(it.text, it.cmd, it.tip));
            return grp;
        }

        private Button MakeSmallButton(string text, string cmd, string tip)
        {
            Button b = new Button
            {
                Text      = text,
                Tag       = cmd,
                Width     = 70,
                Height    = 24,
                Margin    = new Padding(1),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(95, 95, 95),
                Font      = new Font("Segoe UI", 8.25F)
            };
            b.FlatAppearance.BorderColor = Color.Gray;
            b.Click += SendAcadCommand;
            _tip.SetToolTip(b, tip);
            return b;
        }

        private Button MakeBigButton(string text, string cmd, string tip)
        {
            Button b = new Button
            {
                Text      = text,
                Tag       = cmd,
                Width     = 70,
                Height    = 74,
                Margin    = new Padding(2, 2, 6, 2),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Yellow,
                BackColor = Color.FromArgb(85, 85, 85),
                Font      = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            b.FlatAppearance.BorderColor = Color.Gray;
            b.Click += SendAcadCommand;
            _tip.SetToolTip(b, tip);
            return b;
        }

        private void SendAcadCommand(object sender, EventArgs e)
        {
            string cmd = (sender as Button)?.Tag as string;
            if (string.IsNullOrEmpty(cmd)) return;

            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            doc.SendStringToExecute(cmd, true, false, true);
        }
    }
}
