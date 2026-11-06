using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PipeLoad2
{
    // JRevit 의 DuctBuilder.cs DTO 와 짝이다. 형식을 바꾸면 양쪽을 같이 바꿀 것.
    // 길이 단위는 mm, 좌표는 [x, y].

    internal class RevitItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    internal class RevitProject
    {
        public string Document { get; set; } = "";
        public List<RevitItem> Levels { get; set; } = new();
        public List<RevitItem> Systems { get; set; } = new();
        public List<RevitItem> DuctTypes { get; set; } = new();
    }

    internal class RevitDuctGroup
    {
        public string Name { get; set; } = "";
        public int SystemTypeId { get; set; }
        public int DuctTypeId { get; set; }
        public int LevelId { get; set; }
        public double BaseHeight { get; set; }
        public string Align { get; set; } = "";
    }

    internal class RevitDuctLine
    {
        public string Group { get; set; } = "";
        public double[] Start { get; set; } = Array.Empty<double>();
        public double[] End { get; set; } = Array.Empty<double>();
        public double Width { get; set; }
        public double Height { get; set; }
    }

    internal class RevitDuctArc
    {
        public double[] Start { get; set; } = Array.Empty<double>();
        public double[] End { get; set; } = Array.Empty<double>();
    }

    internal class RevitDuctRequest
    {
        public string Document { get; set; } = "";
        public List<RevitDuctGroup> Groups { get; set; } = new();
        public List<RevitDuctLine> Lines { get; set; } = new();
        public List<RevitDuctArc> Arcs { get; set; } = new();
    }

    internal class RevitDuctResult
    {
        public int Created { get; set; }
        public int Elbows { get; set; }
        public List<string> Failures { get; set; } = new();
    }

    /// <summary>JRevit 의 localhost 수신기(DuctBridge)와 통신한다.</summary>
    internal static class DuctToRevitClient
    {
        private const string BaseUrl = "http://localhost:47891/";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // 덕트가 많으면 오래 걸린다. Revit 이 바쁜 경우는 수신기가 10초 만에 오류로 답한다.
        private static readonly HttpClient Http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client.DefaultRequestHeaders.Add("X-JRevit-Client", "Acadv25JArch");
            return client;
        }

        public static Task<RevitProject> GetProjectAsync() =>
            SendAsync<RevitProject>(new HttpRequestMessage(HttpMethod.Get, BaseUrl + "project"));

        public static Task<RevitDuctResult> CreateDuctsAsync(RevitDuctRequest request)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "ducts")
            {
                Content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json")
            };
            return SendAsync<RevitDuctResult>(message);
        }

        private static async Task<T> SendAsync<T>(HttpRequestMessage message)
        {
            string json;
            try
            {
                using HttpResponseMessage response = await Http.SendAsync(message);
                json = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException("Revit 에 연결할 수 없습니다. JRevit 이 설치된 Revit 이 실행 중인지 확인하세요.");
            }

            var reply = JsonSerializer.Deserialize<Reply<T>>(json, JsonOptions);
            if (reply == null || !reply.Ok || reply.Data == null)
                throw new InvalidOperationException(reply?.Error ?? "Revit 응답을 해석할 수 없습니다.");
            return reply.Data;
        }

        private class Reply<T>
        {
            public bool Ok { get; set; }
            public string? Error { get; set; }
            public T? Data { get; set; }
        }

        /// <summary>
        /// XData 크기 문자열의 앞 숫자. SetDuctWidth 는 Text 를 그대로 저장하므로
        /// "600x400" 같은 값이 올 수 있다 (Duct_Outline 명령들과 같은 해석).
        /// </summary>
        public static bool TryParseSize(string? text, out double size)
        {
            size = 0;
            Match m = Regex.Match(text ?? "", @"^\s*(\d+(?:\.\d+)?)");
            return m.Success &&
                   double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out size) &&
                   size > 0;
        }
    }
}
