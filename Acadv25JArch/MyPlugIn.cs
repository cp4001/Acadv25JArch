using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;
using Exception = System.Exception;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(Acadv25JArch.MyPlugin))]

namespace Acadv25JArch
{
    public class MyPlugin : IExtensionApplication
    {
        [DllImport("JArchLicense.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CheckLicense();

        [DllImport("JArchLicense.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetExpirationDate();

        public static bool IsLicenseValid { get; private set; } = false;
        public static DateTime LicenseDate { get; private set; } = DateTime.MinValue;
        private static string _expDate = "";
        private static string _licenseMessage = "";

        private static string logPath = @"C:\Jarch25\assembly_log.txt";

        public void Initialize()
        {
            try
            {
                _expDate = Marshal.PtrToStringAnsi(GetExpirationDate());
                LicenseDate = DateTime.ParseExact(_expDate, "yyyy-MM-dd", null);
                int licenseResult = CheckLicense();

                if (licenseResult == 0)
                {
                    IsLicenseValid = false;
                    _licenseMessage =
                        "\n============================================" +
                        "\n  JArchitecture 라이선스가 만료되었습니다." +
                        $"\n  만료일: {_expDate}" +
                        "\n  프로그램을 사용할 수 없습니다." +
                        "\n  라이선스 갱신은 관리자에게 문의하세요." +
                        "\n============================================\n";
                }
                else
                {
                    IsLicenseValid = true;
                    _licenseMessage =
                        "\n=== JArchitecture 로드됨 ===" +
                        $"\n  라이선스 유효기간: {_expDate} 까지" +
                        "\n============================\n";
                }

                Application.DocumentManager.DocumentCreated += OnDocumentOpened;
                Application.DocumentManager.DocumentActivated += OnFirstDocActivated;
                Application.DocumentManager.DocumentBecameCurrent += OnDocumentOpened;

                // DWG open/active 시 NOD 사전(AINIT_DEFAULTS) 에서 BaseLen 등 도면별 기본값 로드
                var docForLoad = Application.DocumentManager.MdiActiveDocument;
                if (docForLoad != null)
                    PipeDiaCalc.DwgDefaultLoader.LoadDwgDefaults(docForLoad);
                Application.DocumentManager.DocumentCreated      += (s, ev) => PipeDiaCalc.DwgDefaultLoader.LoadDwgDefaults(ev.Document);
                Application.DocumentManager.DocumentActivated    += (s, ev) => PipeDiaCalc.DwgDefaultLoader.LoadDwgDefaults(ev.Document);
                Application.DocumentManager.DocumentBecameCurrent += (s, ev) => PipeDiaCalc.DwgDefaultLoader.LoadDwgDefaults(ev.Document);

                // JArch 리본 탭은 Ainit 실행된 도면에서만 표시 — DLL 로드 시 생성하지 않음.
                // LoadDwgDefaults 가 NOD AINIT_DEFAULTS 존재 여부에 따라 자동 토글.
            }
            catch (Exception ex)
            {
                IsLicenseValid = false;
                _licenseMessage =
                    "\n============================================" +
                    "\n  JArchitecture 라이선스 확인 오류" +
                    $"\n  오류: {ex.Message}" +
                    "\n  프로그램을 사용할 수 없습니다." +
                    "\n============================================\n";
            }
        }

        private static bool _firstDocShown = false;
        private static readonly System.Collections.Generic.HashSet<string> _shownDocs = new();

        private void OnDocumentOpened(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document == null) return;
            string docName = e.Document.Name;
            if (_shownDocs.Contains(docName)) return;
            _shownDocs.Add(docName);
            ShowLicenseMessage(e.Document);
        }

        private void OnFirstDocActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (!_firstDocShown)
            {
                _firstDocShown = true;
                if (e.Document != null)
                {
                    string docName = e.Document.Name;
                    if (!_shownDocs.Contains(docName))
                    {
                        _shownDocs.Add(docName);
                        ShowLicenseMessage(e.Document);
                    }
                }
                Application.DocumentManager.DocumentActivated -= OnFirstDocActivated;
            }
        }

        private void ShowLicenseMessage(Document doc)
        {
            if (doc == null) return;
            Editor ed = doc.Editor;
            if (ed == null) return;

            // NETLOAD/이벤트 콜백 컨텍스트에서 WriteMessage 호출 시 eNotApplicable 발생 가능 →
            // Application.Idle 로 1회 디퍼해 명령 컨텍스트가 빠진 뒤 출력
            void OnIdle(object s, EventArgs e)
            {
                Application.Idle -= OnIdle;
                try { ed.WriteMessage(_licenseMessage); }
                catch { /* 라이선스 메시지 출력 실패는 무시 */ }
            }
            Application.Idle += OnIdle;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //try
            //{
            //    File.AppendAllText(logPath, $"Resolving: {args.Name}\n");

            //    AssemblyName requestedName = new AssemblyName(args.Name);
            //    string assemblyName = requestedName.Name;

            //    // 이미 로드된 어셈블리 확인
            //    var loaded = AppDomain.CurrentDomain.GetAssemblies()
            //        .FirstOrDefault(a => a.GetName().Name == assemblyName);

            //    if (loaded != null)
            //    {
            //        File.AppendAllText(logPath, $"Already loaded: {loaded.FullName}\n");
            //        return loaded;
            //    }

            //    string dllPath = Path.Combine(@"C:\Jarch25", assemblyName + ".dll");

            //    if (File.Exists(dllPath))
            //    {
            //        File.AppendAllText(logPath, $"Loading: {dllPath}\n");

            //        // LoadFile 사용 (더 직접적인 로드)
            //        var asm = Assembly.LoadFile(dllPath);
            //        File.AppendAllText(logPath, $"Loaded: {asm.FullName}\n");
            //        return asm;
            //    }
            //    else
            //    {
            //        File.AppendAllText(logPath, $"Not found: {dllPath}\n");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    File.AppendAllText(logPath, $"Resolve error: {ex.GetType().Name} - {ex.Message}\n");
            //}

            return null;
        }

        public void Terminate()
        {
            //AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }
    }
}
