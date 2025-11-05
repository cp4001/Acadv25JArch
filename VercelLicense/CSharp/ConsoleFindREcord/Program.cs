using System;
using System.Threading.Tasks;
using LicenseCheckLibrary;
namespace LicenseAdmin
{
    /// <summary>
    /// 콘솔에서 라이선스 DB 조회 유틸리티
    /// </summary>
    public class LicenseConsole
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("  License Database Console Viewer");
            Console.WriteLine("==============================================");
            Console.WriteLine();

            if (args.Length > 0 && args[0] == "--help")
            {
                ShowHelp();
                return;
            }

            try
            {
                // 모든 라이선스 조회
                Console.WriteLine("Loading licenses...");
                var licenses = await VercelApiClient.ListAllLicensesAsync();

                Console.WriteLine($"\nTotal Licenses: {licenses.Count}");
                Console.WriteLine(new string('-', 120));
                Console.WriteLine($"{"Machine ID",-40} {"Valid",-10} {"Registered",-20} {"Expires",-20} {"Updated",-20}");
                Console.WriteLine(new string('-', 120));

                foreach (var license in licenses)
                {
                    Console.WriteLine(
                        $"{license.Id,-40} " +
                        $"{(license.Valid ? "✓ Yes" : "✗ No"),-10} " +
                        $"{license.RegisteredAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A",-20} " +
                        $"{license.ExpiresAt?.ToString("yyyy-MM-dd") ?? "Never",-20} " +
                        $"{license.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A",-20}"
                    );
                }

                Console.WriteLine(new string('-', 120));
                Console.WriteLine($"\n✓ Total: {licenses.Count} licenses");

                // 통계 표시
                var validCount = licenses.Count(l => l.Valid);
                var expiredCount = licenses.Count(l => !l.Valid);
                var noExpiryCount = licenses.Count(l => l.ExpiresAt == null);

                Console.WriteLine("\n📊 Statistics:");
                Console.WriteLine($"   Valid:   {validCount}");
                Console.WriteLine($"   Expired: {expiredCount}");
                Console.WriteLine($"   No Expiry: {noExpiryCount}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage: LicenseConsole [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --help    Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  LicenseConsole              List all licenses");
            Console.WriteLine();
        }
    }
}
