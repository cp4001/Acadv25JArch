@echo off
chcp 65001 >nul
echo ========================================
echo Visual Studio 빌드 캐시 완전 삭제
echo ========================================
echo.

cd /d "%~dp0"

echo [1/6] Visual Studio 프로세스 종료 중...
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM msbuild.exe 2>nul
taskkill /F /IM LicenseAdminApp.exe 2>nul
timeout /t 2 >nul
echo ✓ 완료
echo.

echo [2/6] bin 폴더 삭제 중...
if exist bin (
    rd /s /q bin
    echo ✓ bin 폴더 삭제됨
) else (
    echo ✓ bin 폴더 없음
)
echo.

echo [3/6] obj 폴더 삭제 중...
if exist obj (
    rd /s /q obj
    echo ✓ obj 폴더 삭제됨
) else (
    echo ✓ obj 폴더 없음
)
echo.

echo [4/6] .vs 폴더 삭제 중 (Visual Studio 캐시)...
if exist .vs (
    rd /s /q .vs
    echo ✓ .vs 폴더 삭제됨
) else (
    echo ✓ .vs 폴더 없음
)
echo.

echo [5/6] dotnet 캐시 정리 중...
dotnet clean >nul 2>&1
echo ✓ 완료
echo.

echo [6/6] 파일 확인 중...
findstr /C:"JsonPropertyName(\"product\")" VercelApiClient.cs >nul
if %errorlevel% == 0 (
    echo ✓ VercelApiClient.cs에 product 필드 있음
) else (
    echo ✗ VercelApiClient.cs에 product 필드 없음!
    pause
    exit /b 1
)

findstr /C:"JsonPropertyName(\"username\")" VercelApiClient.cs >nul
if %errorlevel% == 0 (
    echo ✓ VercelApiClient.cs에 username 필드 있음
) else (
    echo ✗ VercelApiClient.cs에 username 필드 없음!
    pause
    exit /b 1
)
echo.

echo ========================================
echo ✓ 모든 캐시가 삭제되었습니다!
echo.
echo 이제 Visual Studio를 열고:
echo 1. 솔루션 열기 (LicenseAdminApp.csproj)
echo 2. 빌드 ^> 솔루션 다시 빌드 (Ctrl+Shift+B)
echo 3. 디버깅 시작 (F5)
echo ========================================
pause
