using Autodesk.AutoCAD.Runtime;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Exception = System.Exception;

[assembly: ExtensionApplication(typeof(Acadv25JArch.MyPlugin))]

namespace Acadv25JArch
{
    public class MyPlugin : IExtensionApplication
    {
        private static string logPath = @"C:\Jarch25\assembly_log.txt";

        public void Initialize()
        {
            File.WriteAllText(logPath, "Plugin initialized\n");

            // 리졸브 핸들러를 먼저 등록
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // 그 다음 DLL 로드 (이제 리졸브 핸들러가 의존성을 처리함)
            PreloadAssemblies();
        }

        private static void PreloadAssemblies()
        {
            try
            {
                // 의존성 순서대로 로드
                string[] requiredDlls = new string[]
                {
                    "ExcelNumberFormat.dll",           // 의존성 없음
                    "SixLabors.Fonts.dll",             // 의존성 없음
                    "DocumentFormat.OpenXml.dll",      // 기본 라이브러리
                    "ClosedXML.dll"                    // 위의 것들에 의존
                };

                foreach (var dllName in requiredDlls)
                {
                    string dllPath = Path.Combine(@"C:\Jarch25", dllName);
                    if (File.Exists(dllPath))
                    {
                        try
                        {
                            var asm = Assembly.LoadFrom(dllPath);
                            File.AppendAllText(logPath, $"Preloaded: {asm.FullName}\n");
                        }
                        catch (Exception ex)
                        {
                            File.AppendAllText(logPath, $"Preload failed {dllName}: {ex.Message}\n");
                        }
                    }
                    else
                    {
                        File.AppendAllText(logPath, $"File not found: {dllPath}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"PreloadAssemblies error: {ex.Message}\n");
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                File.AppendAllText(logPath, $"Resolving: {args.Name}\n");

                string assemblyName = new AssemblyName(args.Name).Name;

                // 이미 로드된 어셈블리 확인
                var loaded = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);

                if (loaded != null)
                {
                    File.AppendAllText(logPath, $"Already loaded: {loaded.FullName}\n");
                    return loaded;
                }

                string dllPath = Path.Combine(@"C:\Jarch25", assemblyName + ".dll");

                File.AppendAllText(logPath, $"Looking for: {dllPath}\n");

                if (File.Exists(dllPath))
                {
                    var asm = Assembly.LoadFrom(dllPath);
                    File.AppendAllText(logPath, $"Loaded: {asm.FullName}\n");
                    return asm;
                }
                else
                {
                    File.AppendAllText(logPath, $"Not found: {dllPath}\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"Resolve error: {ex.Message}\n");
            }

            return null;
        }

        public void Terminate()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }
    }
}
