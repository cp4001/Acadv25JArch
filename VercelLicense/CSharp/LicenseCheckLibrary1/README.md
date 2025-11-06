# LicenseCheckLibrary1

라이선스 유효성 검사 라이브러리

## 기능

- 서버에 등록된 머신 ID 확인
- **인터넷 시간**으로 만료일 검사 (로컬 시간 조작 방지)
- 인터넷 연결 없으면 `false` 반환
- 조건 충족 시 `true`, 아니면 `false` 반환

## 호환성

**✅ .NET Standard 2.0**
- .NET Framework 4.6.1 이상
- .NET Framework 4.8.1 ✅
- .NET Core 2.0 이상
- .NET 5, 6, 7, 8 등

## 사용 방법

### 1. 라이브러리 참조

```csharp
using LicenseCheckLibrary1;
```

### 2. 비동기 호출

```csharp
var checker = new LicenseChecker();
bool isValid = await checker.CheckLicenseAsync("MACHINE-ABC-123");

if (isValid)
{
    Console.WriteLine("라이선스 유효");
}
else
{
    Console.WriteLine("라이선스 무효");
}
```

### 3. 동기 호출

```csharp
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

### 4. 커스텀 API URL

```csharp
var checker = new LicenseChecker("https://your-api.com/check-license");
bool isValid = checker.CheckLicense("MACHINE-001");
```

## 반환 조건

### `true` 반환 조건
1. **인터넷 연결 성공**
2. 서버에 해당 ID가 존재
3. `valid` 필드가 `true`
4. **인터넷 시간** ≤ 만료일 (`expiresAt`)

### `false` 반환 조건
- **인터넷 연결 실패** ⭐
- 서버 시간을 가져올 수 없음 ⭐
- ID가 서버에 없음
- `valid` 필드가 `false`
- 인터넷 시간 > 만료일
- API 오류

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
