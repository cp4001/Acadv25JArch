# LicenseManager .NET 8.0 빌드 및 사용 가이드

## 📋 개요

C++/CLI로 작성된 라이선스 관리 라이브러리를 .NET 8.0에서 사용할 수 있도록 변환한 프로젝트입니다.

## 🔧 시스템 요구사항

### 필수 소프트웨어
- **Visual Studio 2022** (버전 17.0 이상)
- **.NET 8.0 SDK**
- **Windows 10 SDK** (최신 버전)

### Visual Studio 필수 워크로드 및 구성요소

Visual Studio Installer를 실행하여 다음 항목을 설치하세요:

#### 워크로드
1. **.NET 데스크톱 개발**
2. **C++를 사용한 데스크톱 개발**

#### 개별 구성요소 (수동 선택 필요)
- **C++/CLI support for v143 build tools (최신)**
- **Windows 10 SDK (10.0.19041.0 이상)**

## 📁 프로젝트 구조

```
LicenseManagerCore/
├── LicenseManagerNet8.sln              # Visual Studio 솔루션
├── LicenseManagerNet8/                 # C++/CLI 라이브러리 프로젝트
│   ├── LicenseManagerNet8.vcxproj     # 프로젝트 파일
│   ├── LicenseManager.h               # 헤더 파일
│   ├── LicenseManager.cpp             # 구현 파일
│   ├── KeyStrings.h                   # 설정 파일
│   └── README.md                      # 상세 문서
└── TestApp/                           # C# 테스트 앱
    ├── TestApp.csproj                 # .NET 8 콘솔 프로젝트
    └── Program.cs                     # 테스트 코드
```

## 🚀 빌드 방법

### 방법 1: Visual Studio에서 빌드

1. **프로젝트 열기**
   ```
   LicenseManagerNet8.sln 파일을 더블클릭하거나
   Visual Studio에서 파일 > 열기 > 프로젝트/솔루션
   ```

2. **빌드 구성 선택**
   - 상단 도구 모음에서 **Debug** 또는 **Release** 선택
   - 플랫폼은 반드시 **x64** 선택

3. **솔루션 빌드**
   - 메뉴: `빌드` > `솔루션 빌드`
   - 단축키: `Ctrl + Shift + B`

4. **빌드 출력 확인**
   - LicenseManagerNet8.dll: `LicenseManagerNet8\x64\Debug\` 또는 `Release\`
   - TestApp.exe: `TestApp\bin\x64\Debug\net8.0\` 또는 `Release\net8.0\`

### 방법 2: 명령줄에서 빌드

```powershell
# Visual Studio 개발자 명령 프롬프트에서 실행
cd C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore

# Debug 빌드
msbuild LicenseManagerNet8.sln /p:Configuration=Debug /p:Platform=x64

# Release 빌드
msbuild LicenseManagerNet8.sln /p:Configuration=Release /p:Platform=x64
```

## 🧪 테스트 방법

### TestApp 실행

1. **솔루션 빌드 완료 후**
   ```
   TestApp을 시작 프로젝트로 설정
   마우스 오른쪽 버튼 > 시작 프로젝트로 설정
   ```

2. **실행**
   - `F5` (디버깅 모드) 또는 `Ctrl + F5` (디버깅하지 않고 시작)

3. **메뉴 사용**
   ```
   1. 라이선스 생성 - 새 라이선스 파일 생성
   2. 라이선스 확인 - 인터넷 시간 기반 유효성 검사
   3. 라이선스 정보 조회 - 만료일 확인
   4. 라이선스 상태 문자열 - 상태 메시지 조회
   0. 종료
   ```

## 💻 사용 방법

### C# 프로젝트에서 라이브러리 사용

#### 1. 프로젝트 참조 추가

**옵션 A: Visual Studio UI 사용**
```
프로젝트 > 참조 추가 > 찾아보기
> LicenseManagerNet8.dll 선택
```

**옵션 B: .csproj 파일 직접 편집**
```xml
<ItemGroup>
  <Reference Include="LicenseManagerNet8">
    <HintPath>경로\LicenseManagerNet8.dll</HintPath>
  </Reference>
</ItemGroup>
```

#### 2. 코드 예제

```csharp
using ProgramLicenseManager;
using System;

class Program
{
    static void Main()
    {
        // 1. 라이선스 생성 (30일 유효)
        DateTime expiration = DateTime.Now.AddDays(30);
        bool created = LicenseHelper.CreateLicense(expiration);
        Console.WriteLine(created ? "생성 완료" : "생성 실패");

        // 2. 라이선스 유효성 검사
        bool isValid = LicenseHelper.CheckLicense();
        Console.WriteLine(isValid ? "유효함" : "유효하지 않음");

        // 3. 라이선스 정보 조회
        DateTime? licenseDate = LicenseHelper.GetLicenseInfo();
        if (licenseDate.HasValue)
        {
            Console.WriteLine($"만료일: {licenseDate:yyyy-MM-dd}");
        }

        // 4. 라이선스 파일 경로 확인
        string path = LicenseHelper.GetLicenseFilePath();
        Console.WriteLine($"라이선스 경로: {path}");

        // 5. 상태 문자열 가져오기
        string status = LicenseHelper.GetDateTime();
        Console.WriteLine(status);
    }
}
```

## 🔐 보안 관련

### 중요: 배포 전 필수 변경사항

**KeyStrings.h 파일 수정**
```cpp
// 현재 (테스트용)
static initonly String^ ENCRYPTION_KEY = "YourSecretKey123";

// 변경 후 (배포용)
static initonly String^ ENCRYPTION_KEY = "복잡하고_긴_암호화키_32자_이상_권장";
```

### 보안 강화 권장사항
1. 암호화 키를 환경변수 또는 안전한 저장소에서 로드
2. 코드 난독화 도구 사용 (ConfuserEx, .NET Reactor 등)
3. 라이선스 파일 저장 위치를 사용자 정의
4. 디지털 서명 추가

## 📝 API 레퍼런스

### LicenseHelper 클래스

| 메서드 | 반환 타입 | 설명 |
|--------|-----------|------|
| `CreateLicense(DateTime)` | `bool` | 라이선스 파일 생성 |
| `CheckLicense()` | `bool` | 라이선스 유효성 검증 (NTP 기반) |
| `GetDateTime()` | `String^` | 라이선스 상태 메시지 반환 |
| `GetLicenseInfo()` | `Nullable<DateTime>` | 만료일 정보 반환 |
| `GetLicenseFilePath()` | `String^` | 라이선스 파일 경로 반환 |

### 라이선스 파일
- **위치**: 실행 파일과 동일한 디렉토리
- **파일명**: `license.dat`
- **형식**: AES 암호화된 Base64 문자열

### 시간 검증
- **NTP 서버**: Google, Windows, NIST 등 다중 서버 사용
- **타임아웃**: 3초
- **목적**: 시스템 시간 조작 방지

## ⚠️ 문제 해결

### 빌드 오류

#### "C1189: CLRSupport NetCore requires..."
**원인**: C++/CLI for .NET Core 지원 미설치  
**해결**: Visual Studio Installer > 개별 구성요소 > "C++/CLI support for v143 build tools" 설치

#### "Windows SDK not found"
**원인**: Windows 10 SDK 미설치  
**해결**: Visual Studio Installer > 개별 구성요소 > "Windows 10 SDK" 설치

#### "Cannot open include file 'vcruntime.h'"
**원인**: MSVC 빌드 도구 미설치  
**해결**: Visual Studio Installer > "C++를 사용한 데스크톱 개발" 워크로드 설치

### 런타임 오류

#### "Could not load file or assembly 'LicenseManagerNet8'"
**원인**: DLL을 찾을 수 없음  
**해결**: 
- DLL이 실행 파일과 같은 폴더에 있는지 확인
- 프로젝트 참조 경로가 올바른지 확인
- x64 플랫폼으로 빌드되었는지 확인

#### "UnauthorizedAccessException" (라이선스 생성 시)
**원인**: 파일 쓰기 권한 부족  
**해결**: 
- 관리자 권한으로 실행
- 다른 폴더로 라이선스 경로 변경
- 폴더 권한 확인

#### "All NTP servers failed to connect"
**원인**: 인터넷 연결 문제 또는 방화벽  
**해결**: 
- 인터넷 연결 확인
- 방화벽에서 UDP 123 포트 허용
- 프록시 설정 확인

## 🔄 .NET Framework에서 .NET 8로의 주요 변경사항

| 항목 | .NET Framework 4.7.2 | .NET 8.0 |
|------|---------------------|----------|
| CLR 지원 | `CLRSupport="true"` | `CLRSupport="NetCore"` |
| 대상 프레임워크 | `TargetFrameworkVersion="v4.7.2"` | `TargetFramework="net8.0"` |
| 어셈블리 속성 | `.NETFramework,Version=v4.7.2.AssemblyAttributes` | 자동 생성 |
| 플랫폼 도구 집합 | v141/v142 | v143 (VS 2022) |

## 📞 추가 정보

### 성능 최적화
- Release 모드로 빌드 시 최적화 적용
- 대용량 데이터 처리 시 메모리 관리 주의

### 배포 체크리스트
- [ ] 암호화 키 변경
- [ ] Release 모드로 빌드
- [ ] 코드 난독화 적용 (선택)
- [ ] 디지털 서명 추가 (선택)
- [ ] 라이선스 파일 경로 검토
- [ ] 테스트 완료

## 📄 라이선스

이 프로젝트는 예제 및 교육 목적으로 제공됩니다. 상용 환경에서 사용 시 적절한 보안 검토와 테스트를 수행하세요.

---

**작성일**: 2025년 10월 28일  
**대상 버전**: .NET 8.0  
**Visual Studio**: 2022 (17.0+)
