using System.Text.Json.Serialization;

namespace JArchLicenseAdmin;

/// <summary>
/// public.licenses 한 행.
///
/// 날짜는 전부 <see cref="string"/> ("yyyy-MM-dd") 로 다룬다. DateTime 으로 바꾸면
/// 로컬 시간대가 개입해 하루씩 밀리는 사고가 난다. 날짜 판정(만료 여부)은 서버의
/// check_license 가 Asia/Seoul 기준으로 하므로 관리 프로그램은 문자열만 나른다.
/// </summary>
public sealed class License
{
    [JsonPropertyName("com_id")]
    public string ComId { get; set; } = "";

    [JsonPropertyName("exp_date")]
    public string ExpDate { get; set; } = "";

    /// <summary>최초 등록일. DB 기본값(오늘, Asia/Seoul)이 넣으므로 쓰기에는 보내지 않는다.</summary>
    [JsonPropertyName("reg_date")]
    public string? RegDate { get; set; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    [JsonPropertyName("comp_name")]
    public string? CompName { get; set; }

    [JsonPropertyName("part_name")]
    public string? PartName { get; set; }
}
