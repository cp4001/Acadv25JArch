using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Management;
using System.Security.Cryptography;

namespace LicenseClient
{
    /// <summary>
    /// Vercel 서버와 통신하여 라이선스를 확인하는 클라이언트
    /// </summary>
    public class LicenseHelper
    {
        private static readonly HttpClient client = new HttpClient();
        
        // ⚠️ 실제 배포된 Vercel URL로 변경하세요!
        private const string API_URL = "https://your-project.vercel.app/api/check-license";

        /// <summary>
        /// 서버에서 암호화 키를 받아옵니다
        /// </summary>
        /// <param name="machineId">머신 고유 ID</param>
        /// <returns>암호화 키</returns>
        public static async Task<string> GetEncryptionKeyFromServer(string machineId)
        {
            try
            {
                var requestBody = new { id = machineId };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(API_URL, content);
                var resultJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<LicenseResponse>(resultJson);
                    
                    if (result?.success == true && result.valid)
                    {
                        Console.WriteLine($"✓ License valid");
                        Console.WriteLine($"  Expires: {result.expiresAt}");
                        return result.key;
                    }
                    else
                    {
                        throw new Exception($"Invalid license: {result?.error}");
                    }
                }
                else
                {
                    var errorResult = JsonSerializer.Deserialize<ErrorResponse>(resultJson);
                    throw new Exception($"License check failed: {errorResult?.error ?? "Unknown error"}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"License verification failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 머신 고유 ID를 생성합니다 (CPU ID + 마더보드 시리얼)
        /// </summary>
        /// <returns>머신 ID</returns>
        public static string GetMachineId()
        {
            try
            {
                string cpuId = GetCpuId();
                string motherboardSerial = GetMotherboardSerial();
                
                // 두 값을 결합하여 해시 생성
                string combined = $"{cpuId}-{motherboardSerial}";
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    string hash = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                    return $"MACHINE-{hash}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not generate hardware-based ID: {ex.Message}");
                // 폴백: 컴퓨터 이름 사용
                return $"MACHINE-{Environment.MachineName}";
            }
        }

        private static string GetCpuId()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["ProcessorId"]?.ToString() ?? "UNKNOWN";
                }
            }
            catch { }
            return "UNKNOWN";
        }

        private static string GetMotherboardSerial()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["SerialNumber"]?.ToString() ?? "UNKNOWN";
                }
            }
            catch { }
            return "UNKNOWN";
        }

        // JSON 응답 모델
        private class LicenseResponse
        {
            public bool success { get; set; }
            public bool valid { get; set; }
            public string key { get; set; }
            public string expiresAt { get; set; }
            public string registeredAt { get; set; }
            public string error { get; set; }
        }

        private class ErrorResponse
        {
            public bool success { get; set; }
            public string error { get; set; }
        }
    }

    /// <summary>
    /// 사용 예제
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== License Client Example ===\n");

                // 1. 머신 ID 생성
                string machineId = LicenseHelper.GetMachineId();
                Console.WriteLine($"Machine ID: {machineId}\n");

                // 2. 서버에서 암호화 키 받기
                Console.WriteLine("Checking license with server...");
                string encryptionKey = await LicenseHelper.GetEncryptionKeyFromServer(machineId);
                
                Console.WriteLine($"\n✓ License validated successfully!");
                Console.WriteLine($"Encryption Key: {encryptionKey}");
                
                // 3. 이제 이 키로 암호화/복호화 작업 수행
                // ... your code here ...

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
