using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LicenseAdminApp
{
    /// <summary>
    /// Vercel 라이선스 서버 API 클라이언트
    /// </summary>
    public class VercelApiClient
    {
        private const string BASE_URL = "https://elec-license.vercel.app";
        private const string ADMIN_KEY = "super-secret-admin-key-change-me-12345"; // ⚠️ 실제 키로 변경 필요
        
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// 라이선스 정보 모델
        /// </summary>
        public class LicenseInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";
            
            [JsonPropertyName("product")]
            public string? Product { get; set; }
            
            [JsonPropertyName("username")]
            public string? Username { get; set; }
            
            [JsonPropertyName("valid")]
            public bool Valid { get; set; }
            
            [JsonPropertyName("registered_at")]
            public DateTime? RegisteredAt { get; set; }
            
            [JsonPropertyName("expires_at")]
            public DateTime? ExpiresAt { get; set; }
            
            [JsonPropertyName("updated_at")]
            public DateTime? UpdatedAt { get; set; }

            // 표시용 속성 (JSON 역직렬화에서 제외)
            [JsonIgnore]
            public string Status => Valid ? "✅ Valid" : "❌ Invalid";
            
            [JsonIgnore]
            public string ProductName => Product ?? "-";
            
            [JsonIgnore]
            public string UserName => Username ?? "-";
            
            [JsonIgnore]
            public string RegisteredDate => RegisteredAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";
            
            [JsonIgnore]
            public string ExpiryDate => ExpiresAt?.ToString("yyyy-MM-dd") ?? "No Expiry";
            
            [JsonIgnore]
            public string UpdatedDate => UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";
            
            [JsonIgnore]
            public int? DaysRemaining => ExpiresAt.HasValue ? (ExpiresAt.Value - DateTime.Now).Days : null;
            
            [JsonIgnore]
            public string DaysLeft => DaysRemaining.HasValue ? $"{DaysRemaining} days" : "∞";
        }

        /// <summary>
        /// 모든 라이선스 조회
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

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var result = JsonSerializer.Deserialize<ListResponse>(resultJson, options);
                    return result?.licenses ?? new List<LicenseInfo>();
                }
                else
                {
                    throw new Exception($"Server error: {resultJson}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to list licenses: {ex.Message}");
            }
        }

        /// <summary>
        /// 새 라이선스 등록
        /// </summary>
        public static async Task RegisterLicenseAsync(
            string id, 
            string? product = null, 
            string? username = null, 
            DateTime? expiresAt = null)
        {
            try
            {
                var requestBody = new
                {
                    adminKey = ADMIN_KEY,
                    id = id,
                    product = product,
                    username = username,
                    expiresAt = expiresAt?.ToString("yyyy-MM-dd")
                };
                
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{BASE_URL}/api/register-id", content);
                var resultJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var error = JsonSerializer.Deserialize<ErrorResponse>(resultJson, options);
                    throw new Exception(error?.error ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to register license: {ex.Message}");
            }
        }

        /// <summary>
        /// 라이선스 삭제
        /// </summary>
        public static async Task DeleteLicenseAsync(string id)
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

                if (!response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var error = JsonSerializer.Deserialize<ErrorResponse>(resultJson, options);
                    throw new Exception(error?.error ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete license: {ex.Message}");
            }
        }

        /// <summary>
        /// 라이선스 수정 (삭제 후 재등록)
        /// </summary>
        public static async Task UpdateLicenseAsync(
            string oldId, 
            string newId, 
            string? product = null, 
            string? username = null, 
            DateTime? expiresAt = null)
        {
            try
            {
                // 기존 ID와 다르면 삭제
                if (oldId != newId)
                {
                    await DeleteLicenseAsync(oldId);
                }
                
                // 새로 등록
                await RegisterLicenseAsync(newId, product, username, expiresAt);
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
