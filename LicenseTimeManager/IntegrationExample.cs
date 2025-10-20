using System;

namespace ProgramLicenseManager
{
    /// <summary>
    /// 실제 프로그램에 라이선스를 통합하는 예제
    /// </summary>
    class IntegrationExample
    {
        static void ExampleMain(string[] args)
        {
            Console.WriteLine("=== 내 프로그램 v1.0 ===\n");

            // ====================================
            // 1단계: 프로그램 시작 시 라이선스 체크
            // ====================================
            if (!LicenseHelper.CheckLicense())
            {
                ShowExpiredMessage();
                return; // 라이선스 없으면 프로그램 종료
            }

            // ====================================
            // 2단계: 라이선스 유효 - 프로그램 실행
            // ====================================
            Console.WriteLine("\n프로그램이 시작됩니다...\n");
            
            // 여기에 실제 프로그램 기능 구현
            RunMainFeatures();
        }

        /// <summary>
        /// 실제 프로그램의 주요 기능
        /// </summary>
        static void RunMainFeatures()
        {
            bool running = true;

            while (running)
            {
                Console.WriteLine("\n=== 메인 메뉴 ===");
                Console.WriteLine("1. 기능 A 실행");
                Console.WriteLine("2. 기능 B 실행");
                Console.WriteLine("3. 라이선스 정보");
                Console.WriteLine("0. 종료");
                Console.Write("\n선택: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        FeatureA();
                        break;
                    case "2":
                        FeatureB();
                        break;
                    case "3":
                        ShowLicenseStatus();
                        break;
                    case "0":
                        running = false;
                        Console.WriteLine("프로그램을 종료합니다.");
                        break;
                    default:
                        Console.WriteLine("잘못된 선택입니다.");
                        break;
                }
            }
        }

        /// <summary>
        /// 중요한 작업 전에 라이선스 재확인 (선택사항)
        /// </summary>
        static void FeatureA()
        {
            Console.WriteLine("\n[기능 A 실행]");
            
            // 중요한 기능 실행 전 라이선스 재확인 (선택사항)
            if (!LicenseHelper.CheckLicense())
            {
                Console.WriteLine("라이선스가 만료되었습니다.");
                return;
            }

            Console.WriteLine("기능 A가 실행되었습니다.");
            // 실제 기능 구현...
        }

        static void FeatureB()
        {
            Console.WriteLine("\n[기능 B 실행]");
            Console.WriteLine("기능 B가 실행되었습니다.");
            // 실제 기능 구현...
        }

        /// <summary>
        /// 라이선스 상태 표시
        /// </summary>
        static void ShowLicenseStatus()
        {
            Console.WriteLine("\n=== 라이선스 정보 ===");
            
            DateTime? expirationDate = LicenseHelper.GetLicenseInfo();
            
            if (expirationDate.HasValue)
            {
                Console.WriteLine($"등록된 라이선스:");
                Console.WriteLine($"  만료일: {expirationDate.Value:yyyy년 MM월 dd일}");
                
                try
                {
                    // 인터넷 시간으로 남은 기간 계산
                    LicenseHelper.CheckLicense();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  경고: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("라이선스가 등록되지 않았습니다.");
            }
        }

        /// <summary>
        /// 만료 메시지 표시
        /// </summary>
        static void ShowExpiredMessage()
        {
            Console.WriteLine("┌─────────────────────────────────────────┐");
            Console.WriteLine("│      라이선스가 유효하지 않습니다       │");
            Console.WriteLine("└─────────────────────────────────────────┘");
            Console.WriteLine();
            Console.WriteLine("프로그램을 계속 사용하려면 라이선스를");
            Console.WriteLine("갱신하거나 구매해 주세요.");
            Console.WriteLine();
            Console.WriteLine("문의: support@yourcompany.com");
            Console.WriteLine("홈페이지: https://www.yourcompany.com");
            Console.WriteLine();
            Console.WriteLine("아무 키나 눌러 종료...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// 더 간단한 통합 예제
    /// </summary>
    class SimpleIntegrationExample
    {
        static void SimpleMain()
        {
            // 한 줄로 간단하게 체크
            if (!LicenseHelper.CheckLicense())
            {
                Console.WriteLine("라이선스 만료됨");
                return;
            }

            // 프로그램 실행
            Console.WriteLine("프로그램 실행 중...");
            DoWork();
        }

        static void DoWork()
        {
            // 실제 작업...
            Console.WriteLine("작업 완료");
        }
    }
}
