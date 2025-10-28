# LicenseManager .NET 8.0 for Windows 11

## 🚀 빠른 시작 (Windows 11)

### 방법 1: PowerShell 스크립트 (가장 쉬움) ⭐⭐⭐

1. **PowerShell을 관리자 권한으로 실행**
   ```
   Win + X > Terminal (관리자) 또는 Windows PowerShell (관리자)
   ```

2. **프로젝트 폴더로 이동**
   ```powershell
   cd C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore
   ```

3. **스크립트 실행**
   ```powershell
   .\QuickBuild.ps1
   ```
   
   또는 Release 빌드:
   ```powershell
   .\QuickBuild.ps1 -Configuration Release
   ```

### 방법 2: 배치 파일 (쉬움) ⭐⭐

1. **탐색기에서 프로젝트 폴더 열기**
   ```
   C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore
   ```

2. **BuildWindows11.bat 마우스 오른쪽 버튼 클릭**
   ```
   관리자 권한으로 실행
   ```

3. **빌드 구성 선택 (Debug 또는 Release)**

### 방법 3: Visual Studio (전통적인 방법) ⭐

1. `LicenseManagerNet8.sln` 더블클릭

2. 플랫폼을 **x64**로 선택

3. `Ctrl + Shift + B` (솔루션 빌드)

## 📋 시스템 요구사항

### 필수
- ✅ Windows 11 (22H2 이상)
- ✅ Visual Studio 2022 (17.8 이상)
- ✅ .NET 8.0 SDK
- ✅ Windows 11 SDK (10.0.22621.0 이상)

### 선택사항
- 개발자 모드 활성화 (권장)
- Windows Terminal (권장)

## 🔧 처음 설정

### 1. Visual Studio 구성요소 확인

**Visual Studio Installer 실행:**
```
시작 > Visual Studio Installer
```

**필수 워크로드:**
- ☑️ .NET 데스크톱 개발
- ☑️ C++를 사용한 데스크톱 개발

**개별 구성요소 (수동 체크):**
- ☑️ C++/CLI support for v143 build tools (.NET Core)
- ☑️ Windows 11 SDK (10.0.22621.0)
- ☑️ MSVC v143 - VS 2022 C++ x64/x86 빌드 도구 (최신)

### 2. .NET 8 SDK 확인

PowerShell에서:
```powershell
dotnet --version
```

8.0.x가 표시되지 않으면:
- https://dotnet.microsoft.com/download/dotnet/8.0

### 3. 개발자 모드 활성화 (권장)

```
설정 > 개인 정보 및 보안 > 개발자용 > 개발자 모드: 켜기
```

## 🐛 문제 해결

### "확인할 수 없는 외부 기호" 오류

**즉시 해결:**
```powershell
# PowerShell 관리자 권한으로
cd C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore
.\CleanBuild.bat
```

그 후 Visual Studio에서 다시 빌드

### "액세스 거부" 오류

**해결 방법:**
1. Visual Studio를 **관리자 권한**으로 실행
2. 또는 BuildWindows11.bat를 **관리자 권한**으로 실행

### Visual Studio에서 프로젝트가 로드되지 않음

**해결 방법:**
```
솔루션 탐색기에서 프로젝트 마우스 오른쪽 버튼
> 프로젝트 다시 로드
```

## 📚 상세 문서

- **[WINDOWS11_BUILD_GUIDE.md](WINDOWS11_BUILD_GUIDE.md)** - 완전한 Windows 11 빌드 가이드
- **[BUILD_GUIDE.md](BUILD_GUIDE.md)** - 일반 빌드 및 사용 가이드
- **[LINKER_ERROR_FIX.md](LINKER_ERROR_FIX.md)** - 링커 오류 해결 방법
- **[LicenseManagerNet8/README.md](LicenseManagerNet8/README.md)** - API 문서

## 📦 프로젝트 구조

```
LicenseManagerCore/
├── 📄 LicenseManagerNet8.sln          # Visual Studio 솔루션
│
├── 🔧 LicenseManagerNet8/             # C++/CLI 라이브러리
│   ├── LicenseManagerNet8.vcxproj
│   ├── LicenseManager.cpp
│   ├── LicenseManager.h
│   └── KeyStrings.h
│
├── 🧪 TestApp/                        # C# 테스트 앱
│   ├── TestApp.csproj
│   └── Program.cs
│
├── 📜 QuickBuild.ps1                  # PowerShell 빌드 스크립트 ⭐
├── 📜 BuildWindows11.bat              # 배치 빌드 스크립트
├── 📜 CleanBuild.bat                  # 정리 스크립트
│
└── 📖 문서/
    ├── WINDOWS11_BUILD_GUIDE.md       # Windows 11 전용 가이드
    ├── BUILD_GUIDE.md                 # 일반 빌드 가이드
    └── LINKER_ERROR_FIX.md           # 문제 해결
```

## 🎯 빌드 출력

빌드 성공 시:
```
x64/Debug/LicenseManagerNet8.dll       (또는 Release)
x64/Debug/LicenseManagerNet8.pdb
```

## 💻 C# 프로젝트에서 사용

### 1. DLL 참조 추가

**Visual Studio:**
```
프로젝트 > 참조 추가 > 찾아보기
> x64\Debug\LicenseManagerNet8.dll 선택
```

### 2. 코드 예제

```csharp
using ProgramLicenseManager;

// 30일 라이선스 생성
DateTime expiration = DateTime.Now.AddDays(30);
bool created = LicenseHelper.CreateLicense(expiration);

// 라이선스 확인
bool isValid = LicenseHelper.CheckLicense();

// 라이선스 정보
DateTime? licenseDate = LicenseHelper.GetLicenseInfo();
```

## 🔐 보안 주의사항

**배포 전 필수:**
1. `KeyStrings.h`의 암호화 키를 변경하세요
2. 코드 난독화 고려
3. 라이선스 파일 위치 검토

## ✅ 빌드 체크리스트

빌드하기 전에 확인:
- [ ] Windows 11 SDK 설치됨
- [ ] Visual Studio 2022 최신 업데이트
- [ ] .NET 8 SDK 설치됨
- [ ] C++/CLI 구성요소 설치됨
- [ ] 플랫폼이 x64로 선택됨
- [ ] 관리자 권한으로 실행 (필요시)

## 🆘 도움이 필요하신가요?

1. **먼저 시도:** `.\QuickBuild.ps1` 실행
2. **여전히 문제?** `WINDOWS11_BUILD_GUIDE.md` 참조
3. **링커 오류?** `LINKER_ERROR_FIX.md` 참조

---

**작성일**: 2025년 10월 28일  
**환경**: Windows 11  
**Visual Studio**: 2022 (v143)  
**.NET**: 8.0
