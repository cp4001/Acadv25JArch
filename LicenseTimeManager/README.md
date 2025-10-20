# C# 프로그램 라이선스 관리 시스템

프로그램 사용 기한을 제한하고 인터넷 시간(NTP)을 기준으로 체크하는 Helper 라이브러리입니다.

## 주요 기능

- ✅ **인터넷 시간 기반 체크**: NTP 서버를 통해 정확한 시간 확인
- ✅ **로컬 시간 조작 방지**: 시스템 시간 변경으로 우회 불가
- ✅ **암호화 저장**: AES 암호화로 라이선스 정보 보호
- ✅ **다중 NTP 서버 지원**: 백업 서버로 안정성 확보

## 빌드 및 실행

### 요구사항
- .NET 6.0 이상
- 인터넷 연결 (NTP 서버 접속용)

### 빌드
```bash
dotnet build
```

### 실행
```bash
dotnet run
```

## 사용 방법

### 1. 라이선스 생성
```csharp
// 2025년 12월 31일까지 유효한 라이선스 생성
DateTime expirationDate = new DateTime(2025, 12, 31);
bool success = LicenseHelper.CreateLicense(expirationDate);
```

### 2. 라이선스 체크
```csharp
// 프로그램 시작 시 체크
if (LicenseHelper.CheckLicense())
{
    // 라이선스 유효 - 프로그램 실행
    RunYourProgram();
}
else
{
    // 라이선스 만료 - 프로그램 종료
    Console.WriteLine("라이선스가 만료되었습니다.");
    return;
}
```

### 3. 실제 프로그램에 적용

```csharp
using ProgramLicenseManager;

class YourProgram
{
    static void Main(string[] args)
    {
        // 프로그램 시작 시 라이선스 체크
        if (!LicenseHelper.CheckLicense())
        {
            Console.WriteLine("라이선스가 유효하지 않습니다.");
            Console.WriteLine("프로그램 구매: https://yourcompany.com");
            return;
        }

        // 라이선스가 유효하면 프로그램 실행
        // 여기에 실제 프로그램 로직 작성
        Console.WriteLine("프로그램 실행 중...");
    }
}
```

## 보안 고려사항

### 강화 방법

1. **암호화 키 변경**
   ```csharp
   private const string ENCRYPTION_KEY = "YourSecretKey123"; 
   // ↑ 이 키를 변경하세요! 
   ```

2. **코드 난독화**
   - 배포 시 코드 난독화 도구 사용 권장
   - ConfuserEx, .NET Reactor 등 사용

3. **추가 검증**
   ```csharp
   // 하드웨어 ID와 결합
   string hwid = GetHardwareId();
   LicenseHelper.CheckLicense(hwid);
   ```

4. **온라인 검증 추가**
   - 주기적으로 서버와 통신하여 라이선스 검증
   - 오프라인 사용 기간 제한

## NTP 서버 목록

기본적으로 다음 NTP 서버를 순차적으로 시도합니다:
- time.google.com
- time.windows.com
- pool.ntp.org
- time.nist.gov

서버 목록은 `LicenseHelper.cs`의 `NTP_SERVERS` 배열에서 수정 가능합니다.

## 파일 설명

- **LicenseHelper.cs**: 라이선스 관리 핵심 로직
- **Program.cs**: 사용 예제 및 테스트 프로그램
- **license.dat**: 생성된 라이선스 파일 (암호화됨)

## 주의사항

⚠️ **중요**: 이 시스템은 기본적인 라이선스 보호 기능을 제공합니다. 
완벽한 보안이 필요한 경우 추가적인 보안 조치를 구현하세요.

- 전문적인 라이선스 관리 솔루션 사용 고려
- 코드 서명 및 무결성 검증 추가
- 하드웨어 기반 보안 모듈 활용

## 라이선스

이 코드는 교육 및 참고 목적으로 제공됩니다.
실제 상용 프로그램에 사용 시 추가적인 보안 강화가 필요합니다.

## 문제 해결

### "모든 NTP 서버 연결에 실패했습니다"
- 인터넷 연결 확인
- 방화벽에서 UDP 포트 123 허용
- 프록시 환경에서는 NTP 서버 접근 제한될 수 있음

### 라이선스 파일이 삭제되었을 때
- 라이선스 파일 백업 권장
- 프로그램 설치 디렉토리 외 안전한 위치에 저장

## 개선 아이디어

1. 하드웨어 ID 바인딩으로 기기 제한
2. 온라인 라이선스 서버 연동
3. 사용 횟수 제한 기능
4. 트라이얼 기간 설정
5. 기능별 라이선스 등급 관리
