using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LicenseCheckLibrary
{
    /// <summary>
    /// Eleclicense 라이선스 검증 라이브러리
    /// </summary>
    public class LicenseChecker
    {
        private const string LICENSE_FILE = "Eleclicense.dat";
        private const string VERCEL_API_URL = "https://elec-license.vercel.app/api/check-license";
        
        private static readonly HttpClient httpClient = new HttpClient();
        
        private static readonly string[] NTP_SERVERS = new[]
        {
            "time.google.com",
            "time.windows.com",
            "pool.ntp.org",
            "time.nist.gov"
        };

        /// <summary>
        /// 라이선스 검증 결과
        /// </summary>
        public class LicenseResult
        {
            public bool IsValid { get; set; }
            public string Message { get; set; } = "";
            public string? MachineId { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public int? RemainingDays { get; set; }
        }

        /// <summary>
        /// 라이선스를 검증합니다
        /// </summary>
        /// <param name="machineId">검증할 머신 ID</param>
        /// <param name="licenseFilePath">라이선스 파일 경로 (null이면 현재 디렉토리)</param>
        /// <returns>검증 결과</returns>
        public static async Task<LicenseResult> CheckLicenseAsync(string machineId, string? licenseFilePath = null)
        {
            try
            {
                // 1. 라이선스 파일 경로 확인
                string filePath = string.IsNullOrWhiteSpace(licenseFilePath)
                    ? Path.Combine(Directory.GetCurrentDirectory(), LICENSE_FILE)
                    : licenseFilePath;

                if (!File.Exists(filePath))
                {
                    return new LicenseResult
                    {
                        IsValid = false,
                        Message = $"License file not found: {filePath}"
                    };
                }

                // 2. Vercel 서버에서 암호화 키 가져오기
                string encryptionKey = await GetEncryptionKeyFromServer(machineId);

                // 3. 라이선스 파일 복호화
                string encryptedData = File.ReadAllText(filePath);
                string decryptedData = DecryptString(encryptedData, encryptionKey);

                // 4. 데이터 파싱 (형식: "ID|yyyy-MM-dd")
                string[] parts = decryptedData.Split('|');
                if (parts.Length != 2)
                {
                    return new LicenseResult
                    {
                        IsValid = false,
                        Message = "Invalid license file format"
                    };
                }

                string storedId = parts[0];
                DateTime expiryDate = DateTime.Parse(parts[1]);

                // 5. ID 검증
                if (storedId != machineId)
                {
                    return new LicenseResult
                    {
                        IsValid = false,
                        Message = "License ID mismatch",
                        MachineId = machineId
                    };
                }

                // 6. 인터넷 시간 가져오기
                DateTime currentTime = await GetInternetTimeAsync();

                // 7. 만료일 검증
                if (currentTime > expiryDate)
                {
                    return new LicenseResult
                    {
                        IsValid = false,
                        Message = "License has expired",
                        MachineId = machineId,
                        ExpiryDate = expiryDate,
                        RemainingDays = 0
                    };
                }

                // 8. 성공
                int remainingDays = (expiryDate - currentTime).Days;
                return new LicenseResult
                {
                    IsValid = true,
                    Message = $"License valid - {remainingDays} days remaining",
                    MachineId = machineId,
                    ExpiryDate = expiryDate,
                    RemainingDays = remainingDays
                };
            }
            catch (HttpRequestException ex)
            {
                return new LicenseResult
                {
                    IsValid = false,
                    Message = $"Server connection failed: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new LicenseResult
                {
                    IsValid = false,
                    Message = $"License verification failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Vercel 서버에서 암호화 키를 가져옵니다
        /// </summary>
        private static async Task<string> GetEncryptionKeyFromServer(string machineId)
        {
            var requestBody = new { id = machineId };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(VERCEL_API_URL, content);
            var resultJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ServerResponse>(resultJson);
                
                if (result?.success == true && result.valid && !string.IsNullOrEmpty(result.key))
                {
                    return result.key;
                }
                else
                {
                    throw new Exception($"Invalid license: {result?.error ?? "Unknown error"}");
                }
            }
            else
            {
                var errorResult = JsonSerializer.Deserialize<ServerResponse>(resultJson);
                throw new Exception($"Server error: {errorResult?.error ?? "Unknown error"}");
            }
        }

        /// <summary>
        /// NTP 서버에서 인터넷 시간을 가져옵니다
        /// </summary>
        private static async Task<DateTime> GetInternetTimeAsync()
        {
            foreach (string ntpServer in NTP_SERVERS)
            {
                try
                {
                    var ntpData = new byte[48];
                    ntpData[0] = 0x1B; // NTP 요청

                    var addresses = await Dns.GetHostAddressesAsync(ntpServer);
                    var ipEndPoint = new IPEndPoint(addresses[0], 123);

                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        socket.ReceiveTimeout = 3000;
                        await socket.ConnectAsync(ipEndPoint);
                        await socket.SendAsync(ntpData, SocketFlags.None);
                        await socket.ReceiveAsync(ntpData, SocketFlags.None);
                    }

                    const int serverReplyTime = 40;
                    ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
                    ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

                    intPart = SwapEndianness(intPart);
                    fractPart = SwapEndianness(fractPart);

                    var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                    var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)milliseconds);

                    return networkDateTime.ToLocalTime();
                }
                catch
                {
                    continue;
                }
            }

            throw new Exception("All NTP servers failed. Check your internet connection.");
        }

        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                          ((x & 0x0000ff00) << 8) +
                          ((x & 0x00ff0000) >> 8) +
                          ((x & 0xff000000) >> 24));
        }

        /// <summary>
        /// 문자열을 복호화합니다
        /// </summary>
        private static string DecryptString(string cipherText, string key)
        {
            byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            byte[] iv = new byte[16];
            Array.Copy(keyBytes, iv, 16);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 서버 응답 모델
        /// </summary>
        private class ServerResponse
        {
            public bool success { get; set; }
            public bool valid { get; set; }
            public string? key { get; set; }
            public string? error { get; set; }
        }
    }
}
