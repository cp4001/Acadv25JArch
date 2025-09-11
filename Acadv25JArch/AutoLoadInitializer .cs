// 어셈블리 특성은 using 절 바로 뒤에 위치해야 합니다.
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using Color = Autodesk.AutoCAD.Colors.Color;
using Exception = System.Exception;

namespace Acadv25JArch
{
    /// <summary>
    /// AutoCAD DLL 자동 로드를 위한 레지스트리 등록/해제 유틸리티
    /// AutoCAD 2025 (.NET 8.0) 대응
    /// </summary>
    public static class AutoCADAutoLoader
    {
        // AutoCAD 2025 레지스트리 경로
        private const string AUTOCAD_REGISTRY_PATH = @"SOFTWARE\Autodesk\AutoCAD\R25.0\ACAD-0001:409\Applications";

        /// <summary>
        /// DLL을 AutoCAD 자동 로드 목록에 등록
        /// </summary>
        /// <param name="dllPath">등록할 DLL의 전체 경로</param>
        /// <param name="appName">애플리케이션 이름 (레지스트리 키 이름)</param>
        /// <param name="description">애플리케이션 설명</param>
        /// <returns>등록 성공 여부</returns>
        public static bool RegisterForAutoLoad(string dllPath, string appName, string description = "")
        {
            try
            {
                // DLL 파일 존재 확인
                if (!File.Exists(dllPath))
                {
                    Console.WriteLine($"DLL 파일을 찾을 수 없습니다: {dllPath}");
                    return false;
                }

                // 레지스트리 키 열기 (없으면 생성)
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey($"{AUTOCAD_REGISTRY_PATH}\\{appName}"))
                {
                    if (key != null)
                    {
                        // 필수 레지스트리 값들 설정
                        key.SetValue("DESCRIPTION", string.IsNullOrEmpty(description) ? appName : description);
                        key.SetValue("LOADCTRLS", 14); // 14 = 자동 로드 + 시작 시 로드
                        key.SetValue("LOADER", dllPath);
                        key.SetValue("MANAGED", 1); // .NET 관리 코드

                        Console.WriteLine($"'{appName}' 애플리케이션이 AutoCAD 자동 로드 목록에 등록되었습니다.");
                        Console.WriteLine($"DLL 경로: {dllPath}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"레지스트리 등록 중 오류 발생: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// DLL을 AutoCAD 자동 로드 목록에서 해제
        /// </summary>
        /// <param name="appName">해제할 애플리케이션 이름</param>
        /// <returns>해제 성공 여부</returns>
        public static bool UnregisterFromAutoLoad(string appName)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AUTOCAD_REGISTRY_PATH, true))
                {
                    if (key != null)
                    {
                        // 서브키가 존재하는지 먼저 확인
                        string[] subKeyNames = key.GetSubKeyNames();
                        if (subKeyNames.Contains(appName))
                        {
                            key.DeleteSubKeyTree(appName);
                            Console.WriteLine($"'{appName}' 애플리케이션이 AutoCAD 자동 로드 목록에서 제거되었습니다.");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"'{appName}' 애플리케이션이 등록되어 있지 않습니다.");
                            return false;
                        }
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"등록되지 않은 애플리케이션입니다: {appName}");
                Console.WriteLine($"상세 오류: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"레지스트리 해제 중 오류 발생: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 현재 실행 중인 어셈블리를 자동 로드 목록에 등록하는 편의 메서드
        /// </summary>
        /// <param name="appName">애플리케이션 이름</param>
        /// <param name="description">애플리케이션 설명</param>
        /// <returns>등록 성공 여부</returns>
        public static bool RegisterCurrentAssembly(string appName, string description = "")
        {
            string currentDllPath = Assembly.GetExecutingAssembly().Location;
            return RegisterForAutoLoad(currentDllPath, appName, description);
        }

        /// <summary>
        /// 등록된 애플리케이션 목록 조회
        /// </summary>
        public static void ListRegisteredApps()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AUTOCAD_REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        Console.WriteLine("=== AutoCAD 자동 로드 등록된 애플리케이션 목록 ===");
                        string[] subKeyNames = key.GetSubKeyNames();

                        foreach (string appName in subKeyNames)
                        {
                            using (RegistryKey appKey = key.OpenSubKey(appName))
                            {
                                if (appKey != null)
                                {
                                    string description = appKey.GetValue("DESCRIPTION")?.ToString() ?? "설명 없음";
                                    string loader = appKey.GetValue("LOADER")?.ToString() ?? "경로 없음";
                                    Console.WriteLine($"- {appName}: {description}");
                                    Console.WriteLine($"  경로: {loader}");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("AutoCAD 애플리케이션 레지스트리 키를 찾을 수 없습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"등록된 애플리케이션 조회 중 오류 발생: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 애플리케이션 초기화 및 종료 처리 클래스
    /// ExtensionApplication 특성으로 자동 등록됨
    /// </summary>
    public class AutoLoadInitializer : IExtensionApplication
    {
        /// <summary>
        /// AutoCAD 시작 시 자동 실행
        /// </summary>
        public void Initialize()
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc?.Editor;

                if (ed != null)
                {
                    ed.WriteMessage("\n=== Line Grouping Tool 로드됨 ===");
                    ed.WriteMessage("\n사용 가능한 명령어:");
                    ed.WriteMessage("\n- GROUPLINES: 기본 라인 그룹핑");
                    ed.WriteMessage("\n- GROUPLINES_CUSTOM: 사용자 정의 허용 각도");
                    ed.WriteMessage("\n- GROUPLINES_STATS: 그룹 통계 정보");
                    ed.WriteMessage("\n- CreateMiddleLine: 평행선 중간선 생성");
                    ed.WriteMessage("\n- REGISTER_AUTOLOAD: DLL 자동 로드 등록");
                    ed.WriteMessage("\n- UNREGISTER_AUTOLOAD: DLL 자동 로드 해제");
                    ed.WriteMessage("\n=====================================\n");
                }

                // 로그 파일에 로드 정보 기록
                LogAutoLoadEvent("Application Initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"등록된 애플리케이션 조회 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// AutoCAD 종료 시 자동 실행
        /// </summary>
        public void Terminate()
        {
            try
            {
                LogAutoLoadEvent("Application Terminated");
            }
            catch (Exception ex)
            {
                // 종료 시에는 조용히 처리
                System.Diagnostics.Debug.WriteLine($"Terminate error: {ex.Message}");
            }
        }

        /// <summary>
        /// 자동 로드 이벤트를 로그 파일에 기록
        /// </summary>
        private void LogAutoLoadEvent(string eventType)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "LineGroupingTool_AutoLoad.log");

                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {eventType} - Version: {GetAssemblyVersion()}\n";
                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // 로그 실패는 무시
            }
        }

        /// <summary>
        /// 어셈블리 버전 정보 가져오기
        /// </summary>
        private string GetAssemblyVersion()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                return assembly.GetName().Version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    /// <summary>
    /// 자동 로드 관리 명령어들
    /// </summary>
    public class AutoLoadManager
    {
        /// <summary>
        /// 현재 DLL을 자동 로드에 등록하는 명령어
        /// </summary>
        [CommandMethod("REGISTER_AUTOLOAD")]
        public void RegisterAutoLoad()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // 현재 DLL 경로 가져오기
                string currentDllPath = Assembly.GetExecutingAssembly().Location;
                string appName = "LineGroupingTool";
                string description = "Line Grouping and Center Line Creation Tool for AutoCAD 2025";

                ed.WriteMessage($"\n현재 DLL 경로: {currentDllPath}");
                ed.WriteMessage("\n자동 로드 등록 중...");

                // 레지스트리에 등록
                bool success = AutoCADAutoLoader.RegisterForAutoLoad(currentDllPath, appName, description);

                if (success)
                {
                    ed.WriteMessage("\n✅ 자동 로드 등록 완료!");
                    ed.WriteMessage("\n📌 AutoCAD를 다시 시작하면 자동으로 로드됩니다.");
                    ed.WriteMessage($"\n등록된 애플리케이션명: {appName}");

                    // 사용자에게 재시작 여부 확인
                    var promptOptions = new PromptKeywordOptions("\nAutoCAD를 다시 시작하여 테스트하시겠습니까? [예(Y)/아니오(N)]")
                    {
                        AllowNone = true
                    };
                    promptOptions.Keywords.Add("Y");
                    promptOptions.Keywords.Add("N");
                    promptOptions.Keywords.Default = "N";

                    var keywordResult = ed.GetKeywords(promptOptions);
                    if (keywordResult.Status == PromptStatus.OK && keywordResult.StringResult.ToUpper() == "Y")
                    {
                        ed.WriteMessage("\nAutoCAD를 종료합니다...");
                        Application.DocumentManager.MdiActiveDocument.SendStringToExecute("QUIT ", true, false, false);
                    }
                }
                else
                {
                    ed.WriteMessage("\n❌ 자동 로드 등록 실패!");
                    ed.WriteMessage("\n관리자 권한으로 실행하거나 권한을 확인하세요.");
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n❌ 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 자동 로드에서 해제하는 명령어
        /// </summary>
        [CommandMethod("UNREGISTER_AUTOLOAD")]
        public void UnregisterAutoLoad()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                string appName = "LineGroupingTool";

                ed.WriteMessage("\n자동 로드 해제 중...");

                bool success = AutoCADAutoLoader.UnregisterFromAutoLoad(appName);

                if (success)
                {
                    ed.WriteMessage("\n✅ 자동 로드 해제 완료!");
                    ed.WriteMessage("\n다음 AutoCAD 시작부터는 자동으로 로드되지 않습니다.");
                }
                else
                {
                    ed.WriteMessage("\n❌ 자동 로드 해제 실패!");
                    ed.WriteMessage("\n등록된 애플리케이션이 없거나 권한이 부족합니다.");
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n❌ 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 등록된 자동 로드 애플리케이션 목록 보기
        /// </summary>
        [CommandMethod("LIST_AUTOLOAD")]
        public void ListAutoLoadApps()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== AutoCAD 자동 로드 애플리케이션 목록 ===");
                AutoCADAutoLoader.ListRegisteredApps();
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n❌ 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 자동 로드 상태 확인
        /// </summary>
        [CommandMethod("CHECK_AUTOLOAD")]
        public void CheckAutoLoadStatus()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                string currentDllPath = Assembly.GetExecutingAssembly().Location;
                string assemblyName = Path.GetFileNameWithoutExtension(currentDllPath);

                ed.WriteMessage("\n=== 자동 로드 상태 확인 ===");
                ed.WriteMessage($"\n현재 DLL: {currentDllPath}");
                ed.WriteMessage($"\n어셈블리명: {assemblyName}");
                ed.WriteMessage($"\n버전: {GetType().Assembly.GetName().Version}");
                ed.WriteMessage($"\n.NET 버전: {Environment.Version}");
                ed.WriteMessage($"\nAutoCAD 버전: {Application.Version}");

                // 로그 파일 위치 표시
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "LineGroupingTool_AutoLoad.log");

                ed.WriteMessage($"\n로그 파일: {logPath}");

                if (File.Exists(logPath))
                {
                    var logInfo = new FileInfo(logPath);
                    ed.WriteMessage($"\n로그 파일 크기: {logInfo.Length} bytes");
                    ed.WriteMessage($"\n마지막 수정: {logInfo.LastWriteTime}");
                }
                else
                {
                    ed.WriteMessage("\n로그 파일이 아직 생성되지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n❌ 오류 발생: {ex.Message}");
            }
        }
    }
}
