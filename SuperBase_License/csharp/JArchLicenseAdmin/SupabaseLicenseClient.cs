using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JArchLicenseAdmin;

/// <summary>
/// public.licenses 에 대한 PostgREST CRUD.
///
/// service_role 키를 쓴다 — RLS 를 전면 우회하므로 테이블을 직접 읽고 쓸 수 있다.
/// 이 프로그램은 관리자 PC 에서만 돌리고 배포하지 않는다는 전제다.
/// 키를 소스에 박지 않고 환경변수에서 읽는 이유는 이 저장소가 커밋되기 때문이다.
/// </summary>
public sealed class SupabaseLicenseClient : IDisposable
{
    public const string ServiceKeyEnvVar = "JARCH_LICENSE_SERVICE_KEY";

    private const string BaseUrl = "https://bvgpukvuygluxternzig.supabase.co/rest/v1/licenses";

    private readonly HttpClient _http;

    public SupabaseLicenseClient(string serviceKey)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.Add("apikey", serviceKey);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceKey);
        _http.DefaultRequestHeaders.Add("Prefer", "return=representation");
    }

    /// <summary>환경변수에서 service_role 키를 읽는다. 없으면 null.</summary>
    public static string? ReadServiceKeyFromEnvironment()
    {
        string? key = Environment.GetEnvironmentVariable(ServiceKeyEnvVar);
        return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
    }

    public async Task<List<License>> ListAsync()
    {
        using var res = await _http.GetAsync($"{BaseUrl}?select=*&order=com_id");
        string body = await res.Content.ReadAsStringAsync();
        Ensure(res, body, "조회");
        return JsonSerializer.Deserialize<List<License>>(body) ?? new List<License>();
    }

    public async Task CreateAsync(License row)
    {
        // reg_date 는 보내지 않는다 — DB 기본값(오늘, Asia/Seoul)이 채운다.
        var payload = new Dictionary<string, string?>
        {
            ["com_id"]    = row.ComId,
            ["exp_date"]  = row.ExpDate,
            ["user_name"] = Nullify(row.UserName),
            ["comp_name"] = Nullify(row.CompName),
            ["part_name"] = Nullify(row.PartName),
        };

        using var content = Json(payload);
        using var res = await _http.PostAsync(BaseUrl, content);
        string body = await res.Content.ReadAsStringAsync();
        Ensure(res, body, "추가");
    }

    public async Task UpdateAsync(string comId, License row)
    {
        // com_id 는 PK 라 갱신 대상에서 뺀다. 바꾸려면 삭제 후 다시 추가해야 한다.
        var payload = new Dictionary<string, string?>
        {
            ["exp_date"]  = row.ExpDate,
            ["user_name"] = Nullify(row.UserName),
            ["comp_name"] = Nullify(row.CompName),
            ["part_name"] = Nullify(row.PartName),
        };

        using var content = Json(payload);
        using var res = await _http.PatchAsync($"{BaseUrl}?com_id=eq.{Escape(comId)}", content);
        string body = await res.Content.ReadAsStringAsync();
        Ensure(res, body, "수정");

        if (body.Trim() == "[]")
            throw new InvalidOperationException($"'{comId}' 를 찾지 못해 아무것도 수정되지 않았습니다.");
    }

    public async Task DeleteAsync(string comId)
    {
        using var res = await _http.DeleteAsync($"{BaseUrl}?com_id=eq.{Escape(comId)}");
        string body = await res.Content.ReadAsStringAsync();
        Ensure(res, body, "삭제");

        if (body.Trim() == "[]")
            throw new InvalidOperationException($"'{comId}' 를 찾지 못해 아무것도 삭제되지 않았습니다.");
    }

    private static StringContent Json(Dictionary<string, string?> payload)
        => new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    /// <summary>빈 문자열은 NULL 로 보낸다. 빈칸과 미입력을 DB 에서 구분하지 않기 위해.</summary>
    private static string? Nullify(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>com_id 를 PostgREST 필터 값으로 넣을 수 있게 인코딩한다.</summary>
    private static string Escape(string value) => Uri.EscapeDataString(value);

    private static void Ensure(HttpResponseMessage res, string body, string what)
    {
        if (res.IsSuccessStatusCode)
            return;

        // PostgREST 오류는 {"message":...,"details":...,"hint":...,"code":...} 로 온다.
        string detail = body;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m))
            {
                detail = m.GetString() ?? body;
                if (doc.RootElement.TryGetProperty("details", out var d) &&
                    d.ValueKind == JsonValueKind.String)
                {
                    detail += Environment.NewLine + d.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // 본문이 JSON 이 아니면 원문 그대로 보여준다.
        }

        throw new InvalidOperationException($"{what} 실패 (HTTP {(int)res.StatusCode}){Environment.NewLine}{detail}");
    }

    public void Dispose() => _http.Dispose();
}
