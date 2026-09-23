using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch.PolyView
{
    /// <summary>
    /// PolyView 관리 팔레트 (Singleton). PaletteSet 이 도킹·자동 숨기기를 제공하므로
    /// 내장 특성 팔레트와 같은 방식으로 좌/우 가장자리에 붙는다.
    /// </summary>
    public static class PolyViewPalette
    {
        private static PaletteSet? _ps;
        private static PolyViewControl? _control;

        // 팔레트 위치/크기를 AutoCAD 가 기억하도록 고정 GUID 사용
        private static readonly Guid PaletteGuid = new Guid("7C2E1D4A-9B36-4F18-A5C0-3E7B41D8F902");

        public static void Show()
        {
            if (_ps == null) Create();

            _ps!.Visible = true;
            _ps.Activate(0);
            _control?.RefreshTree();
        }

        /// <summary>팔레트가 떠 있을 때만 트리를 다시 만든다. 명령 실행 후 호출.</summary>
        public static void RefreshIfVisible()
        {
            if (_ps != null && _ps.Visible) _control?.RefreshTree();
        }

        private static void Create()
        {
            _control = new PolyViewControl();

            _ps = new PaletteSet("PolyView Manager", PaletteGuid)
            {
                MinimumSize = new System.Drawing.Size(220, 300),
                Size = new System.Drawing.Size(300, 500),
                Style = PaletteSetStyles.ShowAutoHideButton
                      | PaletteSetStyles.ShowCloseButton
                      | PaletteSetStyles.Snappable,
                DockEnabled = DockSides.Left | DockSides.Right
            };
            _ps.Add("PolyView", _control);

            // 활성 도면이 바뀌면 그 도면의 트리로 교체
            Application.DocumentManager.DocumentActivated += OnDocumentActivated;
            Application.DocumentManager.DocumentToBeDestroyed += OnDocumentClosing;
        }

        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (_ps != null && _ps.Visible) _control?.RefreshTree();
        }

        private static void OnDocumentClosing(object sender, DocumentCollectionEventArgs e)
        {
            // 닫히는 도면이 현재 표시 중인 도면이면 트리를 비운다.
            // (아직 MdiActiveDocument 가 바뀌기 전이라 여기서는 비우기만 하고,
            //  다음 DocumentActivated 에서 새 도면 기준으로 다시 만든다)
            if (_ps != null && _ps.Visible) _control?.ClearTree();
        }
    }
}
