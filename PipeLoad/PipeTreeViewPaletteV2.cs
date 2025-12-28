using Autodesk.AutoCAD.Windows;
using System;
using System.Windows.Forms.Integration;

namespace PipeLoadAnalysis
{
    /// <summary>
    /// PaletteSet 관리 클래스 (Singleton 패턴) - V2
    /// </summary>
    public class PipeTreeViewPaletteV2
    {
        private static PaletteSet paletteSet;
        private static PipeTreeViewV2 treeViewControl;
        private static ElementHost elementHost;
        private static readonly Guid PALETTE_GUID = new Guid("B2C3D4E5-F6A7-4B5C-8D9E-0F1A2B3C4D5E");

        /// <summary>
        /// PaletteSet 생성 또는 표시
        /// </summary>
        public static void ShowPalette(PipeLineGraph graph, PipeLineNode rootNode)
        {
            if (paletteSet == null)
            {
                // PaletteSet 생성
                paletteSet = new PaletteSet(
                    "배관 네트워크 구조 (Line 기반)",  // 팔레트 제목
                    PALETTE_GUID)
                {
                    MinimumSize = new System.Drawing.Size(300, 400),
                    Size = new System.Drawing.Size(400, 600)
                };

                // WPF UserControl 생성
                treeViewControl = new PipeTreeViewV2();

                // ElementHost를 사용하여 WPF Control을 WinForms에 호스팅
                elementHost = new ElementHost
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    Child = treeViewControl
                };

                // PaletteSet에 ElementHost 추가
                paletteSet.Add("TreeView", elementHost);

                // 닫기 스타일 설정
                paletteSet.Style = PaletteSetStyles.ShowAutoHideButton |
                                  PaletteSetStyles.ShowCloseButton |
                                  PaletteSetStyles.Snappable;

                // 도킹 활성화
                paletteSet.DockEnabled = (DockSides.Left | DockSides.Right | DockSides.Top | DockSides.Bottom);

                // PaletteSet이 닫힐 때 이벤트
                paletteSet.StateChanged += PaletteSet_StateChanged;
            }

            // Graph 데이터 로드
            treeViewControl.LoadPipeGraph(graph, rootNode);

            // PaletteSet 표시
            paletteSet.Visible = true;

            // 활성화 (포커스)
            paletteSet.Activate(0);
        }

        /// <summary>
        /// PaletteSet 숨기기
        /// </summary>
        public static void HidePalette()
        {
            if (paletteSet != null)
            {
                paletteSet.Visible = false;
            }
        }

        /// <summary>
        /// PaletteSet 상태 변경 이벤트
        /// </summary>
        private static void PaletteSet_StateChanged(object sender, PaletteSetStateEventArgs e)
        {
            // PaletteSet이 닫히거나 숨겨질 때 처리
            if (e.NewState == StateEventIndex.Hide)
            {
                // 필요한 경우 리소스 정리
            }
        }

        /// <summary>
        /// PaletteSet이 표시 중인지 확인
        /// </summary>
        public static bool IsVisible()
        {
            return paletteSet != null && paletteSet.Visible;
        }

        /// <summary>
        /// 모든 Node 펼치기
        /// </summary>
        public static void ExpandAll()
        {
            treeViewControl?.ExpandAll();
        }

        /// <summary>
        /// 모든 Node 접기
        /// </summary>
        public static void CollapseAll()
        {
            treeViewControl?.CollapseAll();
        }

        /// <summary>
        /// PaletteSet 제거 (메모리 정리)
        /// </summary>
        public static void Dispose()
        {
            if (paletteSet != null)
            {
                paletteSet.Visible = false;
                paletteSet.Dispose();
                paletteSet = null;
                treeViewControl = null;
                elementHost = null;
            }
        }
    }
}
