using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LicenseCheckLibrary1
{
    public class LicenseChecker
    {
        private readonly string _apiUrl;
        private readonly HttpClient _httpClient;

        public LicenseChecker(string apiUrl = "https://elec-license.vercel.app/api/check-license")
        {
            _apiUrl = apiUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> CheckLicenseAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            try
            {
                var requestData = new { id = id };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(_apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                // 인터넷 시간 가져오기 (서버 응답 헤더)
                DateTimeOffset? serverTime = response.Headers.Date;
                if (serverTime == null)
                {
                    // 인터넷 시간을 가져올 수 없으면 false
                    return false;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject result = JObject.Parse(jsonResponse);

                bool success = result["success"]?.Value<bool>() ?? false;
                bool valid = result["valid"]?.Value<bool>() ?? false;

                if (!success || !valid)
                {
                    return false;
                }

                string expiresAtStr = result["expiresAt"]?.Value<string>();
                
                if (string.IsNullOrEmpty(expiresAtStr))
                {
                    return true;
                }

                if (DateTime.TryParse(expiresAtStr, out DateTime expiresAt))
                {
                    // 인터넷 시간(서버 시간)과 만료일 비교
                    DateTime internetTime = serverTime.Value.DateTime;
                    return internetTime.Date <= expiresAt.Date;
                }

                return false;
            }
            catch (Exception)
            {
                // 인터넷 연결 실패 시 false 반환
                return false;
            }
        }

        public bool CheckLicense(string id)
        {
            return CheckLicenseAsync(id).GetAwaiter().GetResult();
        }
    }
}
