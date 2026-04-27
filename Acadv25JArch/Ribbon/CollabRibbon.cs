using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;              // ICommand
using System.Windows.Media.Imaging;      // BitmapImage
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;                  // RibbonControl, RibbonTab, RibbonPanel...
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Acadv25JArch.Ribbon
{
    /// <summary>
    /// "공동 작업(JArch)" 커스텀 Ribbon 탭 골격.
    ///   패널 1) 공유              : 도면 공유 / 공유 뷰
    ///   패널 2) Autodesk Docs     : Push to Autodesk Docs
    ///   패널 3) 트레이스          : 트레이스 팔레트 / 표식 가져오기
    ///   패널 4) 비교              : DWG 비교
    ///
    /// 자동 등록: <see cref="MyPlugin.Initialize"/> 끝에서 <see cref="EnsureRibbonTab"/> 호출.
    /// 수동 등록/리로드: AutoCAD 명령 "COLLAB_RIBBON_LOAD".
    /// </summary>
    public static class CollabRibbon
    {
        private const string TabId    = "ACADV_COLLAB_JARCH";
        private const string TabTitle = "JArch";

        // 아이콘 폴더 (없으면 텍스트만 표시). 필요 시 절대 경로로 교체.
        private static readonly string IconDir = @"C:\Jarch25\Icons\Collab\";

        private static bool _attached;

        // 공유 패널의 DiaNote 높이 표시용 라벨 (RefreshDiaNoteHeight 로 갱신)
        private static RibbonLabel _diaNoteHeightValue;

        // ───────────────────────── 공개 API ─────────────────────────

        [CommandMethod("COLLAB_RIBBON_LOAD")]
        public static void Cmd_Load() => EnsureRibbonTab();

        [CommandMethod("COLLAB_RIBBON_UNLOAD")]
        public static void Cmd_Unload() => RemoveRibbonTab();

        /// <summary>리본이 준비되면 탭을 1회 등록한다(중복 호출 안전).</summary>
        public static void EnsureRibbonTab()
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon != null)
            {
                BuildTab(ribbon);
                return;
            }
            if (_attached) return;
            _attached = true;
            ComponentManager.ItemInitialized += OnItemInitialized;
        }

        public static void RemoveRibbonTab()
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null) return;
            for (int i = ribbon.Tabs.Count - 1; i >= 0; i--)
                if (ribbon.Tabs[i].Id == TabId)
                    ribbon.Tabs.RemoveAt(i);
        }

        /// <summary>
        /// 탭 표시/숨김 토글.
        /// - visible=true : 필요 시 lazy 생성 후 IsVisible=true. 리본 미준비면 deferred 등록.
        /// - visible=false: 탭이 있으면 IsVisible=false (제거하지 않음). 없으면 no-op.
        /// </summary>
        public static void SetRibbonTabVisible(bool visible)
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null)
            {
                // 리본 미준비 — visible=true 인 경우만 deferred 등록 (이후 ItemInitialized 에서 BuildTab)
                if (visible) EnsureRibbonTab();
                return;
            }

            RibbonTab tab = null;
            foreach (var t in ribbon.Tabs)
            {
                if (t.Id == TabId) { tab = t; break; }
            }

            if (tab == null)
            {
                if (!visible) return; // 없는 탭 숨길 일 없음
                BuildTab(ribbon);
                foreach (var t in ribbon.Tabs)
                {
                    if (t.Id == TabId) { tab = t; break; }
                }
            }

            if (tab != null) tab.IsVisible = visible;
        }

        /// <summary>
        /// 공유 패널의 DiaNote 높이 표시값 갱신.
        /// Ainit / LoadDwgDefaults / SaveBaseLen 직후 호출.
        /// 라벨 미생성(리본 미준비) 상태면 무시.
        /// </summary>
        public static void RefreshDiaNoteHeight()
        {
            if (_diaNoteHeightValue == null) return;
            _diaNoteHeightValue.Text = DiaNote.BaseLen.ToString("0.##");
        }

        // ───────────────────────── 내부 ─────────────────────────

        private static void OnItemInitialized(object sender, RibbonItemEventArgs e)
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null) return;
            ComponentManager.ItemInitialized -= OnItemInitialized;
            _attached = false;
            BuildTab(ribbon);
        }

        private static void BuildTab(RibbonControl ribbon)
        {
            // 같은 Id 의 탭이 이미 있으면 스킵
            foreach (var t in ribbon.Tabs)
                if (t.Id == TabId) return;

            var tab = new RibbonTab
            {
                Id              = TabId,
                Title           = TabTitle,
                Name            = TabTitle,
                IsContextualTab = false,
            };

            tab.Panels.Add(BuildSharePanel());
            tab.Panels.Add(BuildDocsPanel());
            tab.Panels.Add(BuildTracePanel());
            tab.Panels.Add(BuildComparePanel());

            ribbon.Tabs.Add(tab);
        }

        // 패널 1: 공유 ─ LargeButton 2개 + DiaNote 높이 정보 행
        private static RibbonPanel BuildSharePanel()
        {
            var src = new RibbonPanelSource { Title = "공유" };

            // Row 1: 기존 공유 버튼 2개
            var row = new RibbonRowPanel();
            row.Items.Add(LargeButton("도면\n공유", "_SHAREDRAWING",  "share_drawing_32.png"));
            row.Items.Add(LargeButton("공유\n뷰",  "_ONLINESHARE",   "share_view_32.png"));
            src.Items.Add(row);

            // Row 2: DiaNote 높이 표시 + SET 버튼 ("Cmd_SetDiaNoteBase" 명령 호출)
            src.Items.Add(new RibbonRowBreak());
            var infoRow = new RibbonRowPanel();
            infoRow.Items.Add(new RibbonLabel { Text = "DiaNote 높이: " });
            _diaNoteHeightValue = new RibbonLabel
            {
                Text = DiaNote.BaseLen.ToString("0.##"),
                Width = 50,
            };
            infoRow.Items.Add(_diaNoteHeightValue);
            infoRow.Items.Add(new RibbonButton
            {
                Text             = "SET",
                ShowText         = true,
                ShowImage        = false,
                Size             = RibbonItemSize.Standard,
                CommandHandler   = new SendCommandHandler("Cmd_SetDiaNoteBase"),
                CommandParameter = "Cmd_SetDiaNoteBase",
            });
            src.Items.Add(infoRow);

            return new RibbonPanel { Source = src };
        }

        // 패널 2: Autodesk Docs ─ LargeButton 1개
        private static RibbonPanel BuildDocsPanel()
        {
            var src = new RibbonPanelSource { Title = "Autodesk Docs" };
            var row = new RibbonRowPanel();
            row.Items.Add(LargeButton("Push to\nAutodesk Docs", "_PUSHTODOCS", "push_docs_32.png"));
            src.Items.Add(row);
            return new RibbonPanel { Source = src };
        }

        // 패널 3: 트레이스 ─ LargeButton 2개
        private static RibbonPanel BuildTracePanel()
        {
            var src = new RibbonPanelSource { Title = "트레이스" };
            var row = new RibbonRowPanel();
            row.Items.Add(LargeButton("트레이스\n팔레트",   "_TRACEPALETTE",  "trace_palette_32.png"));
            row.Items.Add(LargeButton("표식\n가져오기",     "_IMPORTMARKUPS", "import_markup_32.png"));
            src.Items.Add(row);
            return new RibbonPanel { Source = src };
        }

        // 패널 4: 비교 ─ LargeButton 1개
        private static RibbonPanel BuildComparePanel()
        {
            var src = new RibbonPanelSource { Title = "비교" };
            var row = new RibbonRowPanel();
            row.Items.Add(LargeButton("DWG\n비교", "_COMPARE", "dwg_compare_32.png"));
            src.Items.Add(row);
            return new RibbonPanel { Source = src };
        }

        // ───────────────────────── 헬퍼 ─────────────────────────

        private static RibbonButton LargeButton(string text, string command, string iconFileName)
        {
            return new RibbonButton
            {
                Text             = text,
                ShowText         = true,
                ShowImage        = true,
                Size             = RibbonItemSize.Large,
                Orientation      = System.Windows.Controls.Orientation.Vertical,
                LargeImage       = LoadIcon(iconFileName),
                CommandHandler   = new SendCommandHandler(command),
                CommandParameter = command,
            };
        }

        private static BitmapImage LoadIcon(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            try
            {
                string path = Path.Combine(IconDir, fileName);
                if (!File.Exists(path)) return null;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption  = BitmapCacheOption.OnLoad;
                bmp.UriSource    = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        private sealed class SendCommandHandler : ICommand
        {
            private readonly string _cmd;
            public SendCommandHandler(string cmd) => _cmd = cmd;

            public event EventHandler CanExecuteChanged { add { } remove { } }
            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                doc.SendStringToExecute(_cmd + " ", true, false, true);
            }
        }
    }
}
