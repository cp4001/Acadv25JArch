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
            //File.WriteAllText(logPath, "Plugin initialized\n");
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
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
