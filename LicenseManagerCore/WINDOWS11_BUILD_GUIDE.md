# Windows 11에서 LicenseManager .NET 8 빌드 가이드

## 🪟 Windows 11 환경 최적화 설정

Windows 11에서 C++/CLI .NET 8 프로젝트를 빌드하기 위한 완벽 가이드입니다.

## 📋 Windows 11 전용 체크리스트

### 1️⃣ 필수 구성요소 확인

#### Visual Studio 2022 (17.8 이상 권장)
Windows 11에서는 최신 버전을 사용하세요:
```
도움말 > 업데이트 확인
```

#### .NET 8 SDK 확인
PowerShell 또는 명령 프롬프트에서:
```powershell
dotnet --version
```
**예상 결과**: `8.0.xxx`

설치되지 않은 경우:
```
https://dotnet.microsoft.com/download/dotnet/8.0
```

#### Windows SDK 확인
Visual Studio Installer에서:
- Windows 11 SDK (10.0.22621.0 이상)가 설치되어 있어야 합니다

## 🔧 Windows 11 특화 빌드 방법

### 방법 1: PowerShell 스크립트 사용 (권장) ⭐

1. **PowerShell을 관리자 권한으로 실행**
   - 시작 > PowerShell 검색
   - 마우스 오른쪽 버튼 > 관리자 권한으로 실행

2. **실행 정책 일시 변경** (한 번만 필요)
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
   ```

3. **프로젝트 폴더로 이동**
   ```powershell
   cd C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore
   ```

4. **정리 및 빌드**
   ```powershell
   # 빌드 폴더 정리
   Remove-Item -Path ".vs" -Recurse -Force -ErrorAction SilentlyContinue
   Remove-Item -Path "x64" -Recurse -Force -ErrorAction SilentlyContinue
   Remove-Item -Path "LicenseManagerNet8\x64" -Recurse -Force -ErrorAction SilentlyContinue
   Remove-Item -Path "Debug" -Recurse -Force -ErrorAction SilentlyContinue
   Remove-Item -Path "Release" -Recurse -Force -ErrorAction SilentlyContinue

   # NuGet 캐시 정리
   dotnet nuget locals all --clear

   # Visual Studio 개발자 환경 로드 (경로는 설치 위치에 따라 다를 수 있음)
   & "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\Launch-VsDevShell.ps1"
   
   # 빌드
   msbuild LicenseManagerNet8.sln /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /p:CLRSupport=NetCore
   ```

### 방법 2: Visual Studio에서 빌드

#### 단계별 가이드

1. **Visual Studio 2022 실행**

2. **솔루션 열기**
   ```
   파일 > 열기 > 프로젝트/솔루션
   LicenseManagerNet8.sln 선택
   ```

3. **프로젝트 다시 로드** (프롬프트가 나타나면)
   - "예" 클릭

4. **플랫폼 확인**
   - 상단 도구 모음: **Debug** | **x64** 확인

5. **솔루션 정리**
   ```
   빌드 > 솔루션 정리
   ```

6. **중간 파일 수동 삭제**
   - 탐색기 열기: `Ctrl + Alt + L`
   - 다음 폴더 삭제:
     - `.vs` (숨김 폴더)
     - `x64`
     - `LicenseManagerNet8\x64`

7. **Visual Studio 재시작**

8. **솔루션 다시 빌드**
   ```
   빌드 > 솔루션 다시 빌드
   또는 Ctrl + Shift + B
   ```

### 방법 3: Windows Terminal에서 빌드

Windows 11의 Windows Terminal을 활용:

1. **Windows Terminal 열기**
   - `Win + X` > Terminal (관리자)

2. **개발자 명령 프롬프트 프로필 추가**
   - 설정 > 프로필 추가
   - 명령줄: 
     ```
     %comspec% /k "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
     ```

3. **빌드 실행**
   ```cmd
   cd C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore
   
   REM 정리
   msbuild LicenseManagerNet8.sln /t:Clean /p:Configuration=Debug /p:Platform=x64
   
   REM 빌드
   msbuild LicenseManagerNet8.sln /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /maxcpucount
   ```

## 🎯 Windows 11 특정 문제 해결

### 문제 1: "Windows SDK를 찾을 수 없습니다"

**해결방법:**
1. Visual Studio Installer 실행
2. Visual Studio 2022 > 수정
3. 개별 구성요소 탭
4. 검색: "Windows SDK"
5. **Windows 11 SDK (10.0.22621.0)** 체크 및 설치

### 문제 2: "MSB8020: v143 빌드 도구를 찾을 수 없습니다"

**해결방법:**
1. Visual Studio Installer 실행
2. 수정
3. 개별 구성요소 탭
4. 다음 항목 설치:
   - `MSVC v143 - VS 2022 C++ x64/x86 빌드 도구 (최신)`
   - `C++/CLI support for v143 build tools (.NET Core)`

### 문제 3: "링커 오류 LNK2001"

**해결방법 (Windows 11 최적화):**

CleanBuild.bat 실행 후:

1. **Windows Defender 제외 추가**
   ```
   설정 > 개인 정보 및 보안 > Windows 보안 > 바이러스 및 위협 방지
   > 설정 관리 > 제외 > 제외 추가
   > 폴더 선택: C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore
   ```

2. **개발자 모드 활성화** (권장)
   ```
   설정 > 개인 정보 및 보안 > 개발자용
   > 개발자 모드: 켜기
   ```

### 문제 4: "액세스 거부" 오류

Windows 11의 강화된 보안으로 인한 문제:

**해결방법:**
```powershell
# PowerShell 관리자 권한으로 실행
$folder = "C:\Users\junhoi\Desktop\Work\Tmp\LicenseManagerCore"
$acl = Get-Acl $folder
$permission = "$env:USERNAME","FullControl","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $folder $acl
```

## 🚀 빠른 빌드 스크립트 (Windows 11)

다음 내용으로 `QuickBuild.ps1` 생성:

```powershell
# QuickBuild.ps1 - Windows 11 최적화 빌드 스크립트

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Windows 11 - LicenseManager 빌드 시작" -ForegroundColor Cyan
Write-Host "구성: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 프로젝트 디렉토리로 이동
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# 1. 정리
Write-Host "[1/5] 빌드 폴더 정리 중..." -ForegroundColor Yellow
Remove-Item -Path ".vs" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "x64" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "LicenseManagerNet8\x64" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "Debug" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "Release" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "TestApp\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "TestApp\obj" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  완료" -ForegroundColor Green

# 2. NuGet 캐시 정리
Write-Host "[2/5] NuGet 캐시 정리 중..." -ForegroundColor Yellow
dotnet nuget locals all --clear | Out-Null
Write-Host "  완료" -ForegroundColor Green

# 3. VS 개발 환경 로드
Write-Host "[3/5] Visual Studio 개발 환경 로드 중..." -ForegroundColor Yellow
$vsPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -products * -requires Microsoft.Component.MSBuild `
    -property installationPath

if ($vsPath) {
    $devShell = Join-Path $vsPath "Common7\Tools\Launch-VsDevShell.ps1"
    if (Test-Path $devShell) {
        & $devShell -Arch amd64 -HostArch amd64 | Out-Null
        Write-Host "  완료" -ForegroundColor Green
    }
}

# 4. 빌드
Write-Host "[4/5] 솔루션 빌드 중..." -ForegroundColor Yellow
$buildResult = msbuild LicenseManagerNet8.sln `
    /t:Rebuild `
    /p:Configuration=$Configuration `
    /p:Platform=x64 `
    /p:CLRSupport=NetCore `
    /maxcpucount `
    /verbosity:minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "  빌드 성공!" -ForegroundColor Green
} else {
    Write-Host "  빌드 실패!" -ForegroundColor Red
    exit 1
}

# 5. 결과 확인
Write-Host "[5/5] 빌드 결과 확인..." -ForegroundColor Yellow
$dllPath = "x64\$Configuration\LicenseManagerNet8.dll"
if (Test-Path $dllPath) {
    $dll = Get-Item $dllPath
    Write-Host "  DLL 생성: $($dll.FullName)" -ForegroundColor Green
    Write-Host "  파일 크기: $([math]::Round($dll.Length/1KB, 2)) KB" -ForegroundColor Green
} else {
    Write-Host "  경고: DLL을 찾을 수 없습니다" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "빌드 완료!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
```

**사용 방법:**
```powershell
# Debug 빌드
.\QuickBuild.ps1

# Release 빌드
.\QuickBuild.ps1 -Configuration Release
```

## 📊 Windows 11 성능 최적화 팁

### 1. WSL2 비활성화 (선택사항)
빌드 시 WSL2가 리소스를 많이 사용하는 경우:
```powershell
wsl --shutdown
```

### 2. Windows 검색 인덱싱 제외
```
설정 > 개인 정보 및 보안 > Windows 검색
> 제외된 폴더 추가: 프로젝트 폴더
```

### 3. Visual Studio 성능 최적화
```
도구 > 옵션 > 프로젝트 및 솔루션
- 빌드 및 실행 시 병렬 프로젝트 빌드 수: CPU 코어 수
- MSBuild 프로젝트 빌드 출력 세부 정보: 최소
```

## ✅ 최종 확인사항

빌드 전 체크리스트:
- [ ] Windows 11 SDK 설치됨
- [ ] Visual Studio 2022 최신 버전
- [ ] .NET 8 SDK 설치됨
- [ ] C++/CLI .NET Core 구성요소 설치됨
- [ ] 개발자 모드 활성화 (권장)
- [ ] 프로젝트 폴더 경로에 한글 없음
- [ ] 플랫폼: x64 선택
- [ ] 바이러스 백신 제외 추가 (선택)

행운을 빕니다! 🎉
