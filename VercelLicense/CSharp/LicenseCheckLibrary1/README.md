# LicenseCheckLibrary1

라이선스 유효성 검사 라이브러리

## 기능

- 서버에 등록된 머신 ID 확인
- **인터넷 시간**으로 만료일 검사 (로컬 시간 조작 방지)
- 인터넷 연결 없으면 `false` 반환
- **추가 정보 조회:** 만료일, 사용자 이름

## 호환성

**✅ .NET Standard 2.0**
- .NET Framework 4.6.1 이상
- .NET Framework 4.8.1 ✅
- .NET Core 2.0 이상
- .NET 5, 6, 7, 8 등

## 사용 방법

### 1. 기본 사용 (True/False만)

```csharp
using LicenseCheckLibrary1;

var checker = new LicenseChecker();
bool isValid = checker.CheckLicense("MACHINE-ABC-123");

if (isValid)
{
    Console.WriteLine("라이선스 유효");
}
else
{
    Console.WriteLine("라이선스 무효");
}
```

### 2. 상세 정보 포함 ⭐ NEW

```csharp
using LicenseCheckLibrary1;

var checker = new LicenseChecker();
var result = checker.CheckLicenseWithDetails("MACHINE-ABC-123");

if (result.IsValid)
{
    Console.WriteLine("라이선스 유효");
    Console.WriteLine($"사용자: {result.Username}");
    Console.WriteLine($"만료일: {result.ExpiresAt}");
}
else
{
    Console.WriteLine($"라이선스 무효: {result.ErrorMessage}");
}
```

### 3. 비동기 호출

```csharp
// 기본
bool isValid = await checker.CheckLicenseAsync("MACHINE-ABC-123");

// 상세 정보
var result = await checker.CheckLicenseWithDetailsAsync("MACHINE-ABC-123");
Console.WriteLine($"사용자: {result.Username}");
Console.WriteLine($"만료일: {result.ExpiresAt?.ToString("yyyy-MM-dd")}");
```

### 4. 커스텀 API URL

```csharp
var checker = new LicenseChecker("https://your-api.com/check-license");
var result = checker.CheckLicenseWithDetails("MACHINE-001");
```

## LicenseCheckResult 클래스

```csharp
public class LicenseCheckResult
{
    public bool IsValid { get; set; }           // 유효 여부
    public DateTime? ExpiresAt { get; set; }    // 만료일 (nullable)
    public string Username { get; set; }         // 사용자 이름
    public string ErrorMessage { get; set; }     // 에러 메시지
}
```

## 메서드 목록

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `CheckLicense(id)` | `bool` | 유효성만 확인 (동기) |
| `CheckLicenseAsync(id)` | `Task<bool>` | 유효성만 확인 (비동기) |
| `CheckLicenseWithDetails(id)` | `LicenseCheckResult` | 상세 정보 포함 (동기) ⭐ |
| `CheckLicenseWithDetailsAsync(id)` | `Task<LicenseCheckResult>` | 상세 정보 포함 (비동기) ⭐ |

## 반환 조건

### `IsValid = true` 조건
1. **인터넷 연결 성공**
2. 서버에 해당 ID가 존재
3. `valid` 필드가 `true`
4. **인터넷 시간** ≤ 만료일

### `IsValid = false` 조건
- 인터넷 연결 실패
- 서버 시간을 가져올 수 없음
- ID가 서버에 없음
- `valid` 필드가 `false`
- 인터넷 시간 > 만료일
- API 오류

### ErrorMessage 예시
- "ID is empty"
- "API error: NotFound"
- "Cannot get server time"
- "License not found"
- "License is not valid"
- "License expired"
- "Network error: ..."

## 보안 기능

- ✅ **인터넷 시간 사용** - 로컬 시스템 시간 조작 방지
- ✅ 서버 응답 헤더에서 시간 가져오기
- ✅ 인터넷 연결 필수 (오프라인 사용 불가)

## 빌드

```bash
dotnet build
```

## 의존성

- .NET Standard 2.0
- Newtonsoft.Json 13.0.3
