using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace ProgramLicenseManager
{
    /// <summary>
    /// 프로그램 사용 기한을 관리하는 Helper 클래스
    /// 라이선스 파일: C:\Jarch25\license.dat
    /// </summary>
    public class LicenseHelper
    {
        // 방법 1: 절대 경로로 직접 지정 (간단!)
        private const string LICENSE_FILE = @"C:\Jarch25\license.dat";

        // 방법 2: 폴더와 파일명을 분리 (더 유연함)
        // private const string LICENSE_FOLDER = @"C:\Jarch25";
        // private const string LICENSE_FILE_NAME = "license.dat";
        // private static string LICENSE_FILE => Path.Combine(LICENSE_FOLDER, LICENSE_FILE_NAME);

        private const string ENCRYPTION_KEY = "YourSecretKey123"; // 실제 사용시 변경하세요

        // 대한민국 NTP 서버 목록 (백업용)
        private static readonly string[] NTP_SERVERS = {
            "time.google.com",
            "time.windows.com",
            "pool.ntp.org",
            "time.nist.gov"
        };

        /// <summary>
        /// 라이선스 파일이 저장될 폴더를 확인하고 없으면 생성합니다.
        /// </summary>
        private static void EnsureLicenseDirectoryExists()
        {
            string directory = Path.GetDirectoryName(LICENSE_FILE);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"폴더 생성됨: {directory}");
            }
        }

        /// <summary>
        /// 라이선스를 생성하고 저장합니다.
        /// </summary>
        /// <param name="expirationDate">만료 날짜</param>
        /// <returns>성공 여부</returns>
        public static bool CreateLicense(DateTime expirationDate)
        {
            try
            {
                // 폴더가 없으면 자동 생성
                EnsureLicenseDirectoryExists();

                string licenseData = expirationDate.ToString("yyyy-MM-dd HH:mm:ss");
                string encrypted = EncryptString(licenseData);
                File.WriteAllText(LICENSE_FILE, encrypted);

                Console.WriteLine($"✓ 라이선스 파일 생성: {LICENSE_FILE}");
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"❌ 권한 오류: {LICENSE_FILE}");
                Console.WriteLine("프로그램을 관리자 권한으로 실행하세요.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"라이선스 생성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 프로그램 사용 가능 여부를 체크합니다.
        /// </summary>
        /// <returns>사용 가능하면 true, 만료되었으면 false</returns>
        public static bool CheckLicense()
        {
            try
            {
                // 1. 라이선스 파일 확인
                if (!File.Exists(LICENSE_FILE))
                {
                    Console.WriteLine($"라이선스 파일이 없습니다: {LICENSE_FILE}");
                    return false;
                }

                // 2. 라이선스 파일 읽기
                string encrypted = File.ReadAllText(LICENSE_FILE);
                string decrypted = DecryptString(encrypted);
                DateTime expirationDate = DateTime.Parse(decrypted);

                // 3. 인터넷 시간 가져오기
                DateTime internetTime = GetInternetTime();

                // 4. 만료 확인
                if (internetTime > expirationDate)
                {
                    Console.WriteLine($"라이선스가 만료되었습니다. 만료일: {expirationDate:yyyy-MM-dd}");
                    return false;
                }

                // 5. 남은 기간 표시
                TimeSpan remaining = expirationDate - internetTime;
                Console.WriteLine($"라이선스 유효 - 남은 기간: {remaining.Days}일");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"라이선스 체크 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 라이선스 정보를 조회합니다.
        /// </summary>
        /// <returns>만료일, 라이선스가 없으면 null</returns>
        public static DateTime? GetLicenseInfo()
        {
            try
            {
                if (!File.Exists(LICENSE_FILE))
                    return null;

                string encrypted = File.ReadAllText(LICENSE_FILE);
                string decrypted = DecryptString(encrypted);
                return DateTime.Parse(decrypted);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 라이선스 파일의 전체 경로를 반환합니다.
        /// </summary>
        public static string GetLicenseFilePath()
        {
            return LICENSE_FILE;
        }

        /// <summary>
        /// NTP 서버로부터 인터넷 시간을 가져옵니다.
        /// </summary>
        /// <returns>인터넷 시간</returns>
        private static DateTime GetInternetTime()
        {
            foreach (string ntpServer in NTP_SERVERS)
            {
                try
                {
                    var ntpData = new byte[48];
                    ntpData[0] = 0x1B; // LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

                    var addresses = Dns.GetHostEntry(ntpServer).AddressList;
                    var ipEndPoint = new IPEndPoint(addresses[0], 123);

                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        socket.ReceiveTimeout = 3000; // 3초 타임아웃
                        socket.Connect(ipEndPoint);
                        socket.Send(ntpData);
                        socket.Receive(ntpData);
                    }

                    // NTP 타임스탬프는 1900년 1월 1일부터 시작
                    const byte serverReplyTime = 40;
                    ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
                    ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

                    // 빅 엔디안에서 리틀 엔디안으로 변환
                    intPart = SwapEndianness(intPart);
                    fractPart = SwapEndianness(fractPart);

                    var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                    var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                        .AddMilliseconds((long)milliseconds);

                    return networkDateTime.ToLocalTime();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NTP 서버 {ntpServer} 연결 실패: {ex.Message}");
                    continue; // 다음 서버 시도
                }
            }

            throw new Exception("모든 NTP 서버 연결에 실패했습니다. 인터넷 연결을 확인하세요.");
        }

        /// <summary>
        /// 빅 엔디안 <-> 리틀 엔디안 변환
        /// </summary>
        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                          ((x & 0x0000ff00) << 8) +
                          ((x & 0x00ff0000) >> 8) +
                          ((x & 0xff000000) >> 24));
        }

        /// <summary>
        /// 문자열을 암호화합니다.
        /// </summary>
        private static string EncryptString(string text)
        {
            byte[] key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ENCRYPTION_KEY));
            byte[] iv = new byte[16];
            Array.Copy(key, iv, 16);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(text);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// 암호화된 문자열을 복호화합니다.
        /// </summary>
        private static string DecryptString(string cipherText)
        {
            byte[] key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(ENCRYPTION_KEY));
            byte[] iv = new byte[16];
            Array.Copy(key, iv, 16);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}
