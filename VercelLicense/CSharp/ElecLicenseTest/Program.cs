using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LicenseCheckLibrary1;

namespace ElecLicenseTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║    License Check Test Application                                                                                              ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // 머신 ID 입력
                Console.Write("Enter Machine ID to test: ");
                string machineId = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(machineId))
                {
                    Console.WriteLine("❌ Error: Machine ID cannot be empty!");
                    return;
                }

                //// 라이선스 파일 경로 입력 (선택사항)
                //Console.Write("License file path (press Enter for current directory): ");
                //string licenseFilePath = Console.ReadLine();

                //if (string.IsNullOrWhiteSpace(licenseFilePath))
                //{
                //    licenseFilePath = null; // 현재 디렉토리 사용
                //}

                Console.WriteLine();
                Console.WriteLine("⏳ Checking license...");
                Console.WriteLine();

                // 라이선스 검증
                //var result = await LicenseChecker.CheckLicenseAsync(machineId, licenseFilePath);
                // 결과 출력
                var checker = new LicenseChecker();
                var result = checker.CheckLicenseWithDetails($"{machineId}");

                if (result.IsValid)
                {
                    Console.WriteLine($"유효! 사용자: {result.Username}, 만료일: {result.ExpiresAt}");
                }
                else
                {
                    Console.WriteLine($"무효: {result.ErrorMessage}");
                }


                // 결과 출력
                //if (result.IsValid)
                //{
                //    Console.ForegroundColor = ConsoleColor.Green;
                //    Console.WriteLine("✅ LICENSE VALID");
                //    Console.ResetColor();
                //    Console.WriteLine();
                //    Console.WriteLine($"   Machine ID: {result.MachineId}");
                //    Console.WriteLine($"   Expiry Date: {result.ExpiryDate:yyyy-MM-dd}");
                //    Console.WriteLine($"   Remaining Days: {result.RemainingDays} days");
                //    Console.WriteLine($"   Message: {result.Message}");
                //}
                //else
                //{
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    Console.WriteLine("❌ LICENSE INVALID");
                //    Console.ResetColor();
                //    Console.WriteLine();
                //    Console.WriteLine($"   Reason: {result.Message}");
                //    if (result.MachineId != null)
                //        Console.WriteLine($"   Machine ID: {result.MachineId}");
                //    if (result.ExpiryDate != null)
                //        Console.WriteLine($"   Expiry Date: {result.ExpiryDate:yyyy-MM-dd}");
                //}
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
