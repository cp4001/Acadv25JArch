@echo off
REM Windows 11 최적화 빌드 스크립트
REM 관리자 권한 필요

echo ========================================
echo Windows 11 - LicenseManager 빌드
echo ========================================
echo.

REM 관리자 권한 확인
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [!] 이 스크립트는 관리자 권한이 필요합니다.
    echo [!] 마우스 오른쪽 버튼 ^> "관리자 권한으로 실행"
    echo.
    pause
    exit /b 1
)

cd /d "%~dp0"

REM Visual Studio 경로 찾기
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if not exist "%VSWHERE%" (
    echo [!] Visual Studio 2022를 찾을 수 없습니다.
    echo [!] Visual Studio 2022가 설치되어 있는지 확인하세요.
    pause
    exit /b 1
)

echo [1/6] Visual Studio 2022 검색 중...
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
    set "VSINSTALLDIR=%%i"
)

if not defined VSINSTALLDIR (
    echo [!] Visual Studio 설치 경로를 찾을 수 없습니다.
    pause
    exit /b 1
)

echo   - 찾음: %VSINSTALLDIR%

REM 개발자 명령 프롬프트 환경 설정
echo [2/6] 개발 환경 설정 중...
call "%VSINSTALLDIR%\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 >nul 2>&1
if %errorLevel% neq 0 (
    echo [!] 개발 환경 설정 실패
    pause
    exit /b 1
)
echo   - 완료

REM 정리
echo [3/6] 빌드 폴더 정리 중...
if exist ".vs" rd /s /q ".vs" >nul 2>&1
if exist "x64" rd /s /q "x64" >nul 2>&1
if exist "LicenseManagerNet8\x64" rd /s /q "LicenseManagerNet8\x64" >nul 2>&1
if exist "Debug" rd /s /q "Debug" >nul 2>&1
if exist "Release" rd /s /q "Release" >nul 2>&1
if exist "TestApp\bin" rd /s /q "TestApp\bin" >nul 2>&1
if exist "TestApp\obj" rd /s /q "TestApp\obj" >nul 2>&1
echo   - 완료

REM NuGet 캐시 정리
echo [4/6] NuGet 캐시 정리 중...
dotnet nuget locals all --clear >nul 2>&1
echo   - 완료

REM 빌드 구성 선택
echo [5/6] 빌드 구성 선택:
echo   1. Debug
echo   2. Release
echo.
set /p choice="선택 (1 또는 2, 기본값=1): "

if "%choice%"=="2" (
    set "CONFIG=Release"
) else (
    set "CONFIG=Debug"
)

echo.
echo [6/6] %CONFIG% 빌드 시작...
echo.

msbuild LicenseManagerNet8.sln /t:Rebuild /p:Configuration=%CONFIG% /p:Platform=x64 /p:CLRSupport=NetCore /maxcpucount /verbosity:minimal

if %errorLevel% equ 0 (
    echo.
    echo ========================================
    echo 빌드 성공!
    echo ========================================
    echo.
    echo 출력 위치: x64\%CONFIG%\LicenseManagerNet8.dll
    echo.
    
    if exist "x64\%CONFIG%\LicenseManagerNet8.dll" (
        echo DLL 파일 정보:
        dir "x64\%CONFIG%\LicenseManagerNet8.dll" | find "LicenseManagerNet8.dll"
    )
) else (
    echo.
    echo ========================================
    echo 빌드 실패!
    echo ========================================
    echo.
    echo 문제 해결:
    echo 1. WINDOWS11_BUILD_GUIDE.md 참조
    echo 2. Visual Studio에서 직접 빌드 시도
    echo 3. PowerShell 스크립트 사용: .\QuickBuild.ps1
    echo.
)

echo.
pause
