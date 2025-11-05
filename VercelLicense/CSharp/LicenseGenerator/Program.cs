using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LicenseGenerator
{
    class Program
    {
        // ⚠️ 하드코딩된 암호화 키 (서버 통신 없음)
        private const string ENCRYPTION_KEY = "YourSecretKey123";
        private const string LICENSE_FILE = "Eleclicense.dat";

        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║    License Generator (Eleclicense)    ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // 1. ID 입력
                Console.Write("Enter Machine ID: ");
                string? id = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(id))
                {
                    Console.WriteLine("❌ Error: ID cannot be empty!");
                    return;
                }

                // 2. 만료일 입력
                Console.Write("Enter End Date (yyyy-MM-dd): ");
                string? dateInput = Console.ReadLine();
                
                if (!DateTime.TryParse(dateInput, out DateTime endDate))
                {
                    Console.WriteLine("❌ Error: Invalid date format! Use yyyy-MM-dd");
                    return;
                }

                // 3. 바탕화면 경로
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fullPath = Path.Combine(desktopPath, LICENSE_FILE);

                // 4. 라이선스 파일 생성
                CreateLicenseFile(id, endDate, fullPath);

                Console.WriteLine();
                Console.WriteLine("✅ License file created successfully!");
                Console.WriteLine($"   Location: {fullPath}");
                Console.WriteLine($"   Machine ID: {id}");
                Console.WriteLine($"   Expires: {endDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void CreateLicenseFile(string id, DateTime endDate, string filePath)
        {
            // 데이터 형식: "ID|yyyy-MM-dd"
            string licenseData = $"{id}|{endDate:yyyy-MM-dd}";

            // AES 암호화
            string encryptedData = EncryptString(licenseData);

            // 파일 저장
            File.WriteAllText(filePath, encryptedData);
        }

        private static string EncryptString(string plainText)
        {
            // 키를 SHA256으로 해싱 (32 bytes)
            byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(ENCRYPTION_KEY));
            
            // IV는 키의 처음 16 bytes 사용
            byte[] iv = new byte[16];
            Array.Copy(keyBytes, iv, 16);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
    }
}
