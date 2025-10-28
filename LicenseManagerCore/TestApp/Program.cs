using ProgramLicenseManager;
using System;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== License Manager .NET 8 테스트 ===\n");

            try
            {
                // 라이선스 파일 경로 확인
                string licensePath = LicenseHelper.GetLicenseFilePath();
                Console.WriteLine($"라이선스 파일 경로: {licensePath}\n");

                // 메뉴 표시
                while (true)
                {
                    Console.WriteLine("\n메뉴를 선택하세요:");
                    Console.WriteLine("1. 라이선스 생성");
                    Console.WriteLine("2. 라이선스 확인");
                    Console.WriteLine("3. 라이선스 정보 조회");
                    Console.WriteLine("4. 라이선스 상태 문자열 가져오기");
                    Console.WriteLine("0. 종료");
                    Console.Write("\n선택: ");

                    string? choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            CreateLicenseMenu();
                            break;
                        case "2":
                            CheckLicenseMenu();
                            break;
                        case "3":
                            GetLicenseInfoMenu();
                            break;
                        case "4":
                            GetDateTimeMenu();
                            break;
                        case "0":
                            Console.WriteLine("\n프로그램을 종료합니다.");
                            return;
                        default:
                            Console.WriteLine("\n잘못된 선택입니다.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n오류 발생: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void CreateLicenseMenu()
        {
            Console.WriteLine("\n=== 라이선스 생성 ===");
            Console.Write("유효 기간(일 수)을 입력하세요: ");
            
            if (int.TryParse(Console.ReadLine(), out int days))
            {
                DateTime expirationDate = DateTime.Now.AddDays(days);
                Console.WriteLine($"만료일: {expirationDate:yyyy-MM-dd HH:mm:ss}");
                
                bool result = LicenseHelper.CreateLicense(expirationDate);
                
                if (result)
                {
                    Console.WriteLine("\n✓ 라이선스가 성공적으로 생성되었습니다!");
                }
                else
                {
                    Console.WriteLine("\n❌ 라이선스 생성에 실패했습니다.");
                    Console.WriteLine("관리자 권한으로 실행하거나 파일 권한을 확인하세요.");
                }
            }
            else
            {
                Console.WriteLine("잘못된 입력입니다.");
            }
        }

        static void CheckLicenseMenu()
        {
            Console.WriteLine("\n=== 라이선스 확인 ===");
            Console.WriteLine("인터넷 시간을 확인하고 있습니다...\n");
            
            bool result = LicenseHelper.CheckLicense();
            
            if (result)
            {
                Console.WriteLine("\n✓ 라이선스가 유효합니다!");
            }
            else
            {
                Console.WriteLine("\n❌ 라이선스가 유효하지 않습니다.");
            }
        }

        static void GetLicenseInfoMenu()
        {
            Console.WriteLine("\n=== 라이선스 정보 조회 ===");
            
            DateTime? licenseInfo = LicenseHelper.GetLicenseInfo();
            
            if (licenseInfo.HasValue)
            {
                Console.WriteLine($"라이선스 만료일: {licenseInfo.Value:yyyy-MM-dd HH:mm:ss}");
                
                TimeSpan remaining = licenseInfo.Value - DateTime.Now;
                if (remaining.TotalDays > 0)
                {
                    Console.WriteLine($"남은 시간: {remaining.Days}일 {remaining.Hours}시간");
                }
                else
                {
                    Console.WriteLine("라이선스가 만료되었습니다.");
                }
            }
            else
            {
                Console.WriteLine("라이선스 파일을 찾을 수 없거나 읽을 수 없습니다.");
            }
        }

        static void GetDateTimeMenu()
        {
            Console.WriteLine("\n=== 라이선스 상태 문자열 가져오기 ===");
            Console.WriteLine("인터넷 시간을 확인하고 있습니다...\n");
            
            string result = LicenseHelper.GetDateTime();
            Console.WriteLine($"결과: {result}");
        }
    }
}
