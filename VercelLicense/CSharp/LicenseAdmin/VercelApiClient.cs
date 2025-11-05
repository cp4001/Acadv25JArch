using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace LicenseAdmin
{
    /// <summary>
    /// Vercel 라이선스 서버 API 클라이언트
    /// </summary>
    public class VercelApiClient
    {
        private static readonly string BASE_URL;
        private static readonly string ADMIN_KEY;
        private static readonly HttpClient httpClient;
        private static readonly JsonSerializerOptions jsonOptions;

        static VercelApiClient()
        {
            // App.config에서 설정 읽기
            BASE_URL = ConfigurationManager.AppSettings["ApiBaseUrl"] 
                ?? throw new Exception("ApiBaseUrl not configured in App.config");
            
            ADMIN_KEY = ConfigurationManager.AppSettings["AdminKey"] 
                ?? throw new Exception("AdminKey not configured in App.config");

            // HttpClient 설정
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "LicenseAdmin/1.0");

            // JSON 직렬화 옵션 (개선됨)
            jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,  // 대소문자 구분 안함 (중요!)
                WriteIndented = false
            };
        }

        /// <summary>
        /// 라이선스 정보 모델
        /// </summary>
        public class LicenseInfo
        {
            public string Id { get; set; } = "";
            public bool Valid { get; set; }
            public DateTime? RegisteredAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        /// <summary>
        /// 모든 라이선스 ID 조회
        /// </summary>
        public static async Task<List<LicenseInfo>> ListAllLicensesAsync()
        {
            try
            {
                var requestBody = new { adminKey = ADMIN_KEY };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{BASE_URL}/api/list-ids", content);
                var resultJson = await response.Content.ReadAsStringAsync();

                // 디버깅: API 응답 로깅
                Debug.WriteLine($"API Response Status: {response.StatusCode}");
                Debug.WriteLine($"API Response Body: {resultJson}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ListResponse>(resultJson, jsonOptions);
                    
                    // 디버깅: 파싱된 데이터 확인
                    if (result?.licenses != null)
                    {
                        Debug.WriteLine($"Parsed {result.licenses.Count} licenses");
                        foreach (var lic in result.licenses)
                        {
                            Debug.WriteLine($"License: {lic.Id}, Valid: {lic.Valid}, " +
                                          $"Registered: {lic.RegisteredAt?.ToString() ?? "null"}, " +
                                          $"Expires: {lic.ExpiresAt?.ToString() ?? "null"}, " +
                                          $"Updated: {lic.UpdatedAt?.ToString() ?? "null"}");
                        }
                    }
                    
                    return result?.licenses ?? new List<LicenseInfo>();
                }
                else
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(resultJson, jsonOptions);
                    throw new Exception(error?.error ?? $"Server error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Request timeout. Please check your connection.");
            }
            catch (Exception ex) when (ex is not Exception)
            {
                throw new Exception($"Failed to list licenses: {ex.Message}");
            }
        }

        /// <summary>
        /// 새 라이선스 ID 등록
        /// </summary>
        public static async Task<bool> RegisterLicenseAsync(string id, DateTime? expiresAt)
        {
            try
            {
                var requestBody = new
                {
                    adminKey = ADMIN_KEY,
                    id = id,
                    expiresAt = expiresAt?.ToString("yyyy-MM-dd")
                };
                
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"Register Request: {json}");

                var response = await httpClient.PostAsync($"{BASE_URL}/api/register-id", content);
                var resultJson = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Register Response: {resultJson}");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(resultJson, jsonOptions);
                    throw new Exception(error?.error ?? $"Server error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Request timeout. Please check your connection.");
            }
            catch (Exception ex) when (ex.Message.StartsWith("Network error") || ex.Message.StartsWith("Request timeout"))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to register license: {ex.Message}");
            }
        }

        /// <summary>
        /// 라이선스 ID 삭제
        /// </summary>
        public static async Task<bool> DeleteLicenseAsync(string id)
        {
            try
            {
                var requestBody = new
                {
                    adminKey = ADMIN_KEY,
                    id = id
                };
                
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{BASE_URL}/api/delete-id", content);
                var resultJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(resultJson, jsonOptions);
                    throw new Exception(error?.error ?? $"Server error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Request timeout. Please check your connection.");
            }
            catch (Exception ex) when (ex.Message.StartsWith("Network error") || ex.Message.StartsWith("Request timeout"))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete license: {ex.Message}");
            }
        }

        /// <summary>
        /// 라이선스 ID 수정 (삭제 후 재등록)
        /// </summary>
        public static async Task<bool> UpdateLicenseAsync(string oldId, string newId, DateTime? expiresAt)
        {
            try
            {
                // 기존 ID와 다르면 삭제
                if (oldId != newId)
                {
                    await DeleteLicenseAsync(oldId);
                }
                
                // 새로 등록
                return await RegisterLicenseAsync(newId, expiresAt);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update license: {ex.Message}");
            }
        }

        // JSON 응답 모델
        private class ListResponse
        {
            public bool success { get; set; }
            public int count { get; set; }
            public List<LicenseInfo> licenses { get; set; } = new();
        }

        private class ErrorResponse
        {
            public bool success { get; set; }
            public string? error { get; set; }
        }
    }
}
