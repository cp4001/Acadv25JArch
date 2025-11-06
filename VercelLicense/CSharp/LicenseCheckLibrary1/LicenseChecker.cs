using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LicenseCheckLibrary1
{
    /// <summary>
    /// 라이선스 체크 결과 정보
    /// </summary>
    public class LicenseCheckResult
    {
        /// <summary>
        /// 라이선스 유효 여부
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 만료일
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// 사용자 이름
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 에러 메시지 (실패 시)
        /// </summary>
        public string ErrorMessage { get; set; }
    }

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

        /// <summary>
        /// 라이선스 체크 (상세 정보 포함)
        /// </summary>
        public async Task<LicenseCheckResult> CheckLicenseWithDetailsAsync(string id)
        {
            var result = new LicenseCheckResult
            {
                IsValid = false,
                Username = null,
                ExpiresAt = null,
                ErrorMessage = null
            };

            if (string.IsNullOrWhiteSpace(id))
            {
                result.ErrorMessage = "ID is empty";
                return result;
            }

            try
            {
                var requestData = new { id = id };
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(_apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"API error: {response.StatusCode}";
                    return result;
                }

                // 인터넷 시간 가져오기 (서버 응답 헤더)
                DateTimeOffset? serverTime = response.Headers.Date;
                if (serverTime == null)
                {
                    result.ErrorMessage = "Cannot get server time";
                    return result;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject apiResult = JObject.Parse(jsonResponse);

                bool success = apiResult["success"]?.Value<bool>() ?? false;
                bool valid = apiResult["valid"]?.Value<bool>() ?? false;

                if (!success)
                {
                    result.ErrorMessage = "License not found";
                    return result;
                }

                if (!valid)
                {
                    result.ErrorMessage = "License is not valid";
                    return result;
                }

                // 사용자 이름 가져오기
                result.Username = apiResult["username"]?.Value<string>();

                // 만료일 가져오기
                string expiresAtStr = apiResult["expiresAt"]?.Value<string>();
                
                if (!string.IsNullOrEmpty(expiresAtStr))
                {
                    if (DateTime.TryParse(expiresAtStr, out DateTime expiresAt))
                    {
                        result.ExpiresAt = expiresAt;

                        // 인터넷 시간(서버 시간)과 만료일 비교
                        DateTime internetTime = serverTime.Value.DateTime;
                        if (internetTime.Date > expiresAt.Date)
                        {
                            result.ErrorMessage = "License expired";
                            return result;
                        }
                    }
                }

                // 모든 조건 통과
                result.IsValid = true;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Network error: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 라이선스 체크 (상세 정보 포함 - 동기)
        /// </summary>
        public LicenseCheckResult CheckLicenseWithDetails(string id)
        {
            return CheckLicenseWithDetailsAsync(id).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 라이선스 유효성 검사 (비동기)
        /// </summary>
        public async Task<bool> CheckLicenseAsync(string id)
        {
            var result = await CheckLicenseWithDetailsAsync(id);
            return result.IsValid;
        }

        /// <summary>
        /// 라이선스 유효성 검사 (동기)
        /// </summary>
        public bool CheckLicense(string id)
        {
            return CheckLicenseAsync(id).GetAwaiter().GetResult();
        }
    }
}
