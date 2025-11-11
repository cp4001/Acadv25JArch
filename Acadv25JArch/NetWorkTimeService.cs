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
        /// <summary>
        /// NTP 서버에서 현재 날짜/시간을 가져옵니다
        /// </summary>
        /// <param name="ntpServer">NTP 서버 주소 (기본값: time.windows.com)</param>
        /// <returns>서버에서 가져온 현재 DateTime</returns>
        public static DateTime GetNetworkTime(string ntpServer = "time.windows.com")
        {
            try
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
            catch (Exception ex)
            {
                throw new Exception($"NTP 서버에서 시간을 가져오는데 실패했습니다: {ex.Message}", ex);
            }
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
