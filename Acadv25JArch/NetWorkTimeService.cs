using Autodesk.AutoCAD.Windows.ToolPalette;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetWorkTime
{
    public class NetworkTimeService
    {
        // 여러 NTP 서버 목록 (우선순위 순)
        private static readonly string[] NtpServers = new[]
        {
        "time.windows.com",
        "time.google.com",
        "pool.ntp.org",
        "time.nist.gov",
        "time.cloudflare.com"
    };

        /// <summary>
        /// 여러 NTP 서버를 순차적으로 시도하여 현재 날짜/시간을 가져옵니다
        /// </summary>
        /// <returns>서버에서 가져온 현재 DateTime</returns>
        public static DateTime GetNetworkTime()
        {
            var failedServers = new List<string>();

            foreach (var ntpServer in NtpServers)
            {
                try
                {
                    Console.WriteLine($"[시도] {ntpServer} 서버 연결 중...");

                    var networkTime = GetTimeFromServer(ntpServer);

                    Console.WriteLine($"[성공] {ntpServer} 서버에서 시간을 가져왔습니다.");
                    return networkTime;
                }
                catch (Exception ex)
                {
                    failedServers.Add($"{ntpServer}: {ex.Message}");
                    Console.WriteLine($"[실패] {ntpServer} - {ex.Message}");
                }
            }

            // 모든 서버 실패 시
            throw new Exception($"모든 NTP 서버에서 시간을 가져오는데 실패했습니다.\n실패 목록:\n{string.Join("\n", failedServers)}");
        }

        /// <summary>
        /// 특정 NTP 서버에서 시간을 가져옵니다
        /// </summary>
        private static DateTime GetTimeFromServer(string ntpServer)
        {
            // NTP 데이터는 48바이트
            var ntpData = new byte[48];

            // LI = 0 (경고 없음), VN = 3 (IPv4), Mode = 3 (클라이언트 모드)
            ntpData[0] = 0x1B;

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;
            var ipEndPoint = new IPEndPoint(addresses[0], 123); // NTP는 포트 123 사용

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);
                socket.ReceiveTimeout = 3000; // 3초 타임아웃

                socket.Send(ntpData);
                socket.Receive(ntpData);
            }

            // 서버 응답 시간은 바이트 40-43에 있음
            const byte serverReplyTime = 40;

            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            // 빅 엔디안을 리틀 엔디안으로 변환
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            // NTP 기준 시간은 1900년 1월 1일
            var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                         ((x & 0x0000ff00) << 8) +
                         ((x & 0x00ff0000) >> 8) +
                         ((x & 0xff000000) >> 24));
        }
    }

    // 사용 예시
    class Program
    {
        static void Main()
        {
            try
            {
                DateTime networkTime = NetworkTimeService.GetNetworkTime();
                Console.WriteLine($"현재 날짜/시간: {networkTime}");
                Console.WriteLine($"날짜만: {networkTime.Date}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
            }
        }
    }

}
