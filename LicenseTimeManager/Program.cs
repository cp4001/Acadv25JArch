using System;

namespace ProgramLicenseManager
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 프로그램 라이선스 관리 시스템 ===\n");

            // 메뉴 선택
            Console.WriteLine("1. 라이선스 생성");
            Console.WriteLine("2. 프로그램 실행 (라이선스 체크)");
            Console.WriteLine("3. 라이선스 정보 확인");
            Console.Write("\n선택: ");
            
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CreateLicenseMenu();
                    break;
                case "2":
                    RunProgramWithLicenseCheck();
                    break;
                case "3":
                    ShowLicenseInfo();
                    break;
                default:
                    Console.WriteLine("잘못된 선택입니다.");
                    break;
            }

            Console.WriteLine("\n아무 키나 눌러 종료...");
            Console.ReadKey();
        }

        /// <summary>
        /// 라이선스 생성 메뉴
        /// </summary>
        static void CreateLicenseMenu()
        {
            Console.WriteLine("\n=== 라이선스 생성 ===");
            Console.Write("만료 날짜를 입력하세요 (예: 2025-12-31): ");
            string dateInput = Console.ReadLine();

            try
            {
                DateTime expirationDate = DateTime.Parse(dateInput);
                
                if (expirationDate <= DateTime.Now)
                {
                    Console.WriteLine("만료 날짜는 현재 날짜보다 이후여야 합니다.");
                    return;
                }

                bool success = LicenseHelper.CreateLicense(expirationDate);
                
                if (success)
                {
                    Console.WriteLine($"✓ 라이선스가 생성되었습니다.");
                    Console.WriteLine($"  만료일: {expirationDate:yyyy-MM-dd}");
                }
                else
                {
                    Console.WriteLine("✗ 라이선스 생성에 실패했습니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 라이선스 체크 후 프로그램 실행
        /// </summary>
        static void RunProgramWithLicenseCheck()
        {
            Console.WriteLine("\n=== 프로그램 실행 ===");
            Console.WriteLine("라이선스를 확인하는 중...\n");

            bool isValid = LicenseHelper.CheckLicense();

            if (isValid)
            {
                Console.WriteLine("\n✓ 라이선스 유효 - 프로그램을 실행합니다.");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                
                // 여기에 실제 프로그램 로직을 작성하세요
                RunYourActualProgram();
                
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            else
            {
                Console.WriteLine("\n✗ 라이선스가 유효하지 않습니다.");
                Console.WriteLine("프로그램을 실행할 수 없습니다.");
                Console.WriteLine("\n라이선스 구매 문의: example@company.com");
            }
        }

        /// <summary>
        /// 실제 프로그램 로직 (예시)
        /// </summary>
        static void RunYourActualProgram()
        {
            Console.WriteLine("프로그램이 실행 중입니다...");
            Console.WriteLine("이곳에 실제 프로그램 기능을 구현하세요.");
            
            // 예시 작업
            for (int i = 1; i <= 5; i++)
            {
                Console.WriteLine($"작업 진행 중... {i}/5");
                System.Threading.Thread.Sleep(500);
            }
            
            Console.WriteLine("작업 완료!");
        }

        /// <summary>
        /// 라이선스 정보 확인
        /// </summary>
        static void ShowLicenseInfo()
        {
            Console.WriteLine("\n=== 라이선스 정보 ===");
            
            DateTime? expirationDate = LicenseHelper.GetLicenseInfo();
            
            if (expirationDate.HasValue)
            {
                Console.WriteLine($"만료일: {expirationDate.Value:yyyy-MM-dd HH:mm:ss}");
                
                // 인터넷 시간 기준 남은 기간 계산
                try
                {
                    bool isValid = LicenseHelper.CheckLicense();
                    
                    if (!isValid)
                    {
                        Console.WriteLine("상태: 만료됨");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"인터넷 시간 확인 실패: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("라이선스 파일이 없습니다.");
            }
        }
    }
}
