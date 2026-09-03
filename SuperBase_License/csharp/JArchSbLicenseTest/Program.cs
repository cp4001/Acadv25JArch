using System.Diagnostics;
using System.Reflection;

namespace JArchSbLicenseTest;

/// <summary>
/// C++ DLL(JArchSbLicense.dll)을 C# 에서 P/Invoke 로 부르는 샘플 겸 회귀 검증.
///
///   JArchSbLicenseTest.exe            → TEST-* 회귀 검증
///   JArchSbLicenseTest.exe --id       → 이 PC 의 고유 ID 만 출력
///   JArchSbLicenseTest.exe ABC-1234   → 단건 조회 (종료코드 0=유효, 1=차단)
/// </summary>
internal static class Program
{
    private record Case(string ComId, bool Expected, string Note);

    // prd.md §5.1 반환 규칙. C++ test_client.cpp 와 같은 케이스를 쓴다.
    private static readonly Case[] Cases =
    {
        new("TEST-VALID",   true,  "exp_date = 오늘+30"),
        new("TEST-TODAY",   true,  "exp_date = 오늘 (당일은 사용 가능)"),
        new("TEST-EXPIRED", false, "exp_date = 오늘-1"),
        new("NO-SUCH-ID",   false, "미등록"),
        new("",             false, "빈 문자열 (요청 안 보냄)"),
    };

    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string dllPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
            "JArchSbLicense.dll");

        if (!File.Exists(dllPath))
        {
            Console.Error.WriteLine($"JArchSbLicense.dll 이 없습니다: {dllPath}");
            Console.Error.WriteLine("cpp\\build.bat 을 먼저 실행하세요.");
            return 2;
        }

        string? machineId = NativeLicense.GetMachineId();

        if (args.Length > 0 && args[0] == "--id")
        {
            if (machineId is null)
            {
                Console.Error.WriteLine("GetMachineId 실패");
                return 2;
            }
            Console.WriteLine(machineId);
            return 0;
        }

        Console.WriteLine($"프로세스   : {(Environment.Is64BitProcess ? "x64" : "x86")}");
        Console.WriteLine($"네이티브   : {dllPath}");
        Console.WriteLine($"고유 ID    : {machineId ?? "(실패)"}");
        Console.WriteLine();

        if (args.Length > 0)
        {
            bool ok = NativeLicense.IsValid(args[0]);
            Console.WriteLine($"{args[0]} -> {(ok ? "true (사용 가능)" : "false (차단)")}");
            return ok ? 0 : 1;
        }

        Console.WriteLine("JArchSbLicense P/Invoke 회귀 검증");
        Console.WriteLine(new string('-', 62));

        int failed = 0;
        foreach (Case c in Cases)
        {
            var sw = Stopwatch.StartNew();
            bool got = NativeLicense.IsValid(c.ComId);
            sw.Stop();

            bool ok = got == c.Expected;
            if (!ok)
                failed++;

            Console.WriteLine(
                $"[{(ok ? "OK  " : "FAIL")}] {(c.ComId.Length > 0 ? c.ComId : "(빈값)"),-14} " +
                $"기대={c.Expected.ToString().ToLowerInvariant(),-5} " +
                $"실제={got.ToString().ToLowerInvariant(),-5} " +
                $"{sw.ElapsedMilliseconds,4}ms  {c.Note}");
        }

        Console.WriteLine(new string('-', 62));
        Console.WriteLine(failed == 0
            ? "전부 통과"
            : $"{failed}건 실패 — 서버 상태(ACTIVE_HEALTHY)와 네트워크를 확인할 것");

        return failed == 0 ? 0 : 1;
    }
}
