# LicenseManager .NET 8 프로젝트

이 프로젝트는 C++/CLI로 작성된 라이선스 관리 라이브러리를 .NET 8에서 사용할 수 있도록 변환한 버전입니다.

## 시스템 요구사항

- Visual Studio 2022 (17.0 이상)
- .NET 8.0 SDK
- C++/CLI 지원 (.NET 데스크톱 개발 워크로드 포함)
- Windows SDK 10.0

## Visual Studio 설치 시 필요한 구성요소

Visual Studio Installer에서 다음 항목을 설치해야 합니다:

1. **.NET 데스크톱 개발** 워크로드
2. **C++를 사용한 데스크톱 개발** 워크로드
3. 개별 구성요소:
   - C++/CLI support for v143 build tools (최신 버전)
   - Windows 10 SDK (최신 버전)

## 프로젝트 열기 및 빌드

1. Visual Studio 2022를 실행합니다.
2. `LicenseManagerNet8.sln` 파일을 엽니다.
3. 솔루션 구성을 선택합니다 (Debug 또는 Release).
4. 플랫폼을 x64로 선택합니다.
5. 솔루션 빌드: `Ctrl+Shift+B` 또는 메뉴에서 빌드 > 솔루션 빌드

## 주요 변경사항

### .NET Framework 4.7.2 → .NET 8.0

이 프로젝트는 원래의 .NET Framework 4.7.2 버전을 .NET 8.0으로 변환했습니다.

**변경된 프로젝트 설정:**
- `TargetFramework`: net8.0
- `CLRSupport`: NetCore
- `PlatformToolset`: v143 (Visual Studio 2022)

## 사용 방법

### C# 프로젝트에서 참조하기

1. C# .NET 8 프로젝트를 생성합니다.
2. 프로젝트 참조에 `LicenseManagerNet8.dll`을 추가합니다.
3. 다음과 같이 사용합니다:

```csharp
using ProgramLicenseManager;
using System;

class Program
{
    static void Main()
    {
        // 라이선스 생성 (관리자 권한 필요)
        DateTime expirationDate = DateTime.Now.AddDays(30);
        bool created = LicenseHelper.CreateLicense(expirationDate);
        
        if (created)
        {
            Console.WriteLine("라이선스가 생성되었습니다.");
        }
        
        // 라이선스 확인
        bool isValid = LicenseHelper.CheckLicense();
        
        if (isValid)
        {
            Console.WriteLine("라이선스가 유효합니다.");
        }
        else
        {
            Console.WriteLine("라이선스가 유효하지 않습니다.");
        }
        
        // 라이선스 정보 가져오기
        DateTime? licenseInfo = LicenseHelper.GetLicenseInfo();
        if (licenseInfo.HasValue)
        {
            Console.WriteLine($"만료일: {licenseInfo.Value:yyyy-MM-dd}");
        }
    }
}
```

## 기능 설명

### LicenseHelper 클래스

#### Public 메서드

- `CreateLicense(DateTime expirationDate)`: 라이선스 파일 생성
- `CheckLicense()`: 라이선스 유효성 검증 (인터넷 시간 기반)
- `GetDateTime()`: 라이선스 상태 문자열 반환
- `GetLicenseInfo()`: 라이선스 만료일 반환
- `GetLicenseFilePath()`: 라이선스 파일 경로 반환

#### 보안 기능

- AES 암호화를 사용한 라이선스 파일 보호
- NTP 서버를 통한 실제 시간 확인 (시스템 시간 조작 방지)
- SHA256 해시를 사용한 키 생성

## 파일 구조

```
LicenseManagerNet8/
├── LicenseManagerNet8.sln          # Visual Studio 솔루션 파일
└── LicenseManagerNet8/
    ├── LicenseManagerNet8.vcxproj  # C++/CLI 프로젝트 파일
    ├── LicenseManager.h             # 헤더 파일
    ├── LicenseManager.cpp           # 구현 파일
    └── KeyStrings.h                 # 설정 및 상수
```

## 빌드 출력

빌드 성공 시 다음 파일들이 생성됩니다:
- `LicenseManagerNet8.dll` - 관리되는 어셈블리
- `LicenseManagerNet8.pdb` - 디버그 심볼

## 문제 해결

### 빌드 오류: "CLRSupport NetCore requires..."

**해결 방법**: Visual Studio 2022의 C++/CLI 지원 구성요소가 설치되어 있는지 확인하세요.

### 오류: "Cannot find Windows SDK"

**해결 방법**: Visual Studio Installer에서 최신 Windows 10 SDK를 설치하세요.

### 런타임 오류: ".NET runtime not found"

**해결 방법**: .NET 8.0 Runtime이 설치되어 있는지 확인하세요.

## 주의사항

1. **플랫폼 대상**: 이 프로젝트는 x64 플랫폼만 지원합니다.
2. **관리자 권한**: 라이선스 파일 생성 시 관리자 권한이 필요할 수 있습니다.
3. **인터넷 연결**: 라이선스 검증 시 NTP 서버 접근을 위해 인터넷 연결이 필요합니다.
4. **암호화 키**: `KeyStrings.h`의 `ENCRYPTION_KEY`는 실제 배포 시 강력한 키로 변경하세요.

## 보안 고려사항

배포 전에 다음 사항을 검토하세요:

1. **암호화 키 변경**: `KeyStrings.h`의 기본 암호화 키를 변경하세요.
2. **코드 난독화**: 리버스 엔지니어링 방지를 위해 추가 보호 조치를 고려하세요.
3. **라이선스 파일 위치**: 필요에 따라 라이선스 파일 저장 위치를 변경하세요.

## 라이선스

이 코드는 예제 목적으로 제공됩니다. 실제 프로덕션 환경에서 사용 시 적절한 보안 검토를 수행하세요.
