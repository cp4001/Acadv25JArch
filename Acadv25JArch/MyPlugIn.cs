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
        // 라이선스는 JArchXData.arx 가 전담한다 — 인터넷 시각(HTTPS Date 헤더) + 레지스트리
        // 롤백 방어, 만료일은 C++ 소스에 XOR 은닉. arx 가 세션당 1회만 시각을 확정하므로
        // 아래 조회는 첫 호출에서만 네트워크를 탄다.
        private static JArch.Xdata.LicenseInfo? _lic;

        private static JArch.Xdata.LicenseInfo? Lic
        {
            get
            {
                if (_lic == null)
                {
                    try { _lic = JArch.Xdata.GetInfo(); }
                    catch { /* arx 로드 실패 - 호출부에서 null 로 처리 */ }
                }
                return _lic;
            }
        }

        public static bool IsLicenseValid => Lic is { Usable: true };
        public static DateTime LicenseDate => Lic?.EndDate ?? DateTime.MinValue;

        /// <summary>등록 후 다시 확인할 수 있게 캐시를 버린다(JARCLICENSE 가 부른다).</summary>
        public static void ResetLicenseCache() => _lic = null;

        private static string _initError = "";

        /// <summary>명령창에 출력할 라이선스 배너. 최초 호출에서 시각이 확정된다.</summary>
        private static string LicenseMessage()
        {
            if (_initError.Length > 0)
                return "\n============================================" +
                       "\n  JArchitecture 초기화 오류" +
                       $"\n  오류: {_initError}" +
                       "\n  프로그램을 사용할 수 없습니다." +
                       "\n============================================\n";

            var lic = Lic;
            if (lic == null)
                return "\n============================================" +
                       "\n  JArchitecture 라이선스 확인 오류" +
                       "\n  JArchXData.arx 를 로드하지 못했습니다." +
                       "\n  프로그램을 사용할 수 없습니다." +
                       "\n============================================\n";

            switch (lic.Status)
            {
                case JArch.Xdata.LicenseStatus.Expired:
                    return "\n============================================" +
                           "\n  JArchitecture 라이선스가 만료되었습니다." +
                           $"\n  만료일: {lic.EndDate:yyyy-MM-dd}" +
                           "\n  프로그램을 사용할 수 없습니다." +
                           "\n  라이선스 갱신은 관리자에게 문의하세요." +
                           "\n============================================\n";

                case JArch.Xdata.LicenseStatus.NotRegistered:
                    return "\n============================================" +
                           "\n  등록된 사용자가 아닙니다." +
                           $"\n  이 컴퓨터의 ID: {JArch.Xdata.GetMachineId() ?? "(확인 실패)"}" +
                           "\n  프로그램을 사용할 수 없습니다." +
                           "\n  JARCLICENSE 명령으로 등록하세요." +
                           "\n============================================\n";

                case JArch.Xdata.LicenseStatus.Unreachable:
                    return "\n============================================" +
                           "\n  라이선스 서버에 연결할 수 없습니다." +
                           "\n  인터넷 연결을 확인한 뒤 AutoCAD 를 다시 시작하세요." +
                           "\n  프로그램을 사용할 수 없습니다." +
                           "\n============================================\n";
            }

            return "\n=== JArchitecture 로드됨 ===" +
                   $"\n  라이선스 유효기간: {lic.EndDate:yyyy-MM-dd} 까지" +
                   $"\n  (확인 {lic.NowUtc.ToLocalTime():yyyy-MM-dd})" +
                   "\n============================\n";
        }

        private static string logPath = @"C:\Jarch25\assembly_log.txt";

        public void Initialize()
        {
            try
            {
                // DLL 로드 시점에 JArchXData.arx 를 함께 올린다.
                // 이 호출 자체는 네트워크를 타지 않는다(시각 확정은 첫 GetInfo/Set 때).
                // 참조로만 쓰는 어셈블리는 JArchXDataNet 의 Startup 이 자동 실행되지 않으므로
                // 여기서 명시적으로 호출해야 한다.
                JArch.Xdata.EnsureArxLoaded();

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
                _initError = ex.Message;
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
                try { ed.WriteMessage(LicenseMessage()); }
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
