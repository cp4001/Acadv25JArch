using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Security.Principal;

namespace DiskSerialChecker
{
    public class DiskInfo
    {
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string InterfaceType { get; set; }
        public long SizeGB { get; set; }
        public int Index { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== 하드디스크 시리얼 번호 확인 프로그램 ===");
            Console.WriteLine($"실행 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

            // 관리자 권한 확인
            if (!IsAdministrator())
            {
                Console.WriteLine("⚠️  경고: 일부 정보는 관리자 권한이 필요할 수 있습니다.");
                Console.WriteLine();
            }

            // 1. 모든 디스크 정보 출력
            Console.WriteLine("[ 모든 디스크 정보 ]\n");
            List<DiskInfo> allDisks = GetAllDiskInfo();
            DisplayDiskInfo(allDisks);

            // 2. 시스템 드라이브(C:)의 물리 디스크 시리얼
            Console.WriteLine("\n[ 시스템 드라이브(C:) 정보 ]\n");
            string systemDiskSerial = GetSystemDiskSerial();
            if (!string.IsNullOrEmpty(systemDiskSerial))
            {
                Console.WriteLine($"시스템 디스크 시리얼: {systemDiskSerial}");
            }
            else
            {
                Console.WriteLine("시스템 디스크 시리얼을 가져올 수 없습니다.");
            }

            // 3. 볼륨 시리얼 (참고용)
            Console.WriteLine("\n[ 볼륨 시리얼 번호 (파일시스템 레벨) ]\n");
            string volumeSerial = GetVolumeSerial("C:");
            Console.WriteLine($"C: 드라이브 볼륨 시리얼: {volumeSerial}");

            // 4. 디스크 지문 (Fingerprint)
            Console.WriteLine("\n[ 디스크 지문 정보 ]\n");
            string fingerprint = GenerateDiskFingerprint();
            if (!string.IsNullOrEmpty(fingerprint))
            {
                Console.WriteLine($"디스크 지문 (원본): {fingerprint}");
                
                string fingerprintHash = GenerateDiskFingerprintHash();
                Console.WriteLine($"디스크 지문 (SHA256): {fingerprintHash}");
            }
            else
            {
                Console.WriteLine("디스크 지문을 생성할 수 없습니다.");
            }

            Console.WriteLine("\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("프로그램을 종료하려면 아무 키나 누르세요...");
            Console.ReadKey();
        }

        /// <summary>
        /// 관리자 권한으로 실행 중인지 확인합니다.
        /// </summary>
        private static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 모든 물리 디스크의 정보를 가져옵니다.
        /// </summary>
        public static List<DiskInfo> GetAllDiskInfo()
        {
            List<DiskInfo> diskList = new List<DiskInfo>();

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Index, Model, SerialNumber, InterfaceType, Size FROM Win32_DiskDrive"))
                {
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        foreach (ManagementObject disk in results)
                        {
                            using (disk)
                            {
                                DiskInfo info = new DiskInfo
                                {
                                    Index = Convert.ToInt32(disk["Index"]),
                                    Model = disk["Model"]?.ToString() ?? "Unknown",
                                    SerialNumber = disk["SerialNumber"]?.ToString()?.Trim() ?? "N/A",
                                    InterfaceType = disk["InterfaceType"]?.ToString() ?? "Unknown",
                                    SizeGB = disk["Size"] != null ?
                                             Convert.ToInt64(disk["Size"]) / (1024 * 1024 * 1024) : 0
                                };

                                diskList.Add(info);
                            }
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine($"WMI 오류 발생: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }

            return diskList;
        }

        /// <summary>
        /// 시스템 드라이브(C:)가 속한 물리 디스크의 시리얼 번호를 가져옵니다.
        /// </summary>
        public static string GetSystemDiskSerial()
        {
            try
            {
                // 1단계: C: 드라이브가 속한 파티션 찾기
                using (ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher(
                    "ASSOCIATORS OF {Win32_LogicalDisk.DeviceID='C:'} " +
                    "WHERE AssocClass=Win32_LogicalDiskToPartition"))
                {
                    using (ManagementObjectCollection partitions = partitionSearcher.Get())
                    {
                        foreach (ManagementObject partition in partitions)
                        {
                            using (partition)
                            {
                                // 2단계: 파티션이 속한 물리 디스크 찾기
                                using (ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher(
                                    "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" +
                                    partition["DeviceID"] + "'} " +
                                    "WHERE AssocClass=Win32_DiskDriveToDiskPartition"))
                                {
                                    using (ManagementObjectCollection disks = diskSearcher.Get())
                                    {
                                        foreach (ManagementObject disk in disks)
                                        {
                                            using (disk)
                                            {
                                                string serial = disk["SerialNumber"]?.ToString()?.Trim();
                                                if (!string.IsNullOrEmpty(serial))
                                                {
                                                    return serial;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine($"시스템 디스크 시리얼 조회 WMI 오류: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"시스템 디스크 시리얼 조회 오류: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 특정 드라이브의 볼륨 시리얼 번호를 가져옵니다. (파일시스템 레벨)
        /// </summary>
        public static string GetVolumeSerial(string driveLetter)
        {
            try
            {
                // 드라이브 문자 정규화 (예: "C:" 또는 "C" 모두 허용)
                if (!driveLetter.EndsWith(":"))
                {
                    driveLetter += ":";
                }

                using (ManagementObject disk = new ManagementObject(
                    $"win32_logicaldisk.deviceid='{driveLetter}'"))
                {
                    disk.Get();
                    return disk["VolumeSerialNumber"]?.ToString() ?? "N/A";
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine($"볼륨 시리얼 조회 WMI 오류: {ex.Message}");
                return "N/A";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"볼륨 시리얼 조회 오류: {ex.Message}");
                return "N/A";
            }
        }

        /// <summary>
        /// 디스크 정보를 보기 좋게 출력합니다.
        /// </summary>
        private static void DisplayDiskInfo(List<DiskInfo> disks)
        {
            if (disks == null || disks.Count == 0)
            {
                Console.WriteLine("디스크 정보를 찾을 수 없습니다.");
                return;
            }

            foreach (var disk in disks)
            {
                Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"디스크 #{disk.Index}");
                Console.WriteLine($"  모델명:       {disk.Model}");
                Console.WriteLine($"  시리얼 번호:  {disk.SerialNumber}");
                Console.WriteLine($"  인터페이스:   {disk.InterfaceType}");
                Console.WriteLine($"  용량:         {disk.SizeGB} GB");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 하드디스크 시리얼 번호만 간단히 가져오기 (첫 번째 디스크)
        /// </summary>
        public static string GetFirstDiskSerial()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_DiskDrive"))
                {
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        foreach (ManagementObject disk in results)
                        {
                            using (disk)
                            {
                                string serial = disk["SerialNumber"]?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(serial))
                                {
                                    return serial;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 모든 디스크 시리얼을 조합한 고유 ID 생성
        /// </summary>
        public static string GenerateDiskFingerprint()
        {
            try
            {
                List<string> serials = new List<string>();

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_DiskDrive ORDER BY Index"))
                {
                    using (ManagementObjectCollection results = searcher.Get())
                    {
                        foreach (ManagementObject disk in results)
                        {
                            using (disk)
                            {
                                string serial = disk["SerialNumber"]?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(serial))
                                {
                                    serials.Add(serial);
                                }
                            }
                        }
                    }
                }

                // 정렬하여 일관성 유지
                serials.Sort();

                // 조합하여 하나의 문자열로
                return string.Join("|", serials);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 디스크 지문을 SHA256 해시로 변환합니다.
        /// </summary>
        public static string GenerateDiskFingerprintHash()
        {
            try
            {
                string fingerprint = GenerateDiskFingerprint();
                if (string.IsNullOrEmpty(fingerprint))
                {
                    return string.Empty;
                }

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
                    return BitConverter.ToString(bytes).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"해시 생성 오류: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 특정 시리얼 번호를 SHA256 해시로 변환합니다.
        /// </summary>
        public static string HashSerial(string serial)
        {
            if (string.IsNullOrEmpty(serial))
            {
                return string.Empty;
            }

            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(serial));
                    return BitConverter.ToString(bytes).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"해시 생성 오류: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
