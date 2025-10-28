@echo off
echo ========================================
echo LicenseManagerNet8 빌드 폴더 정리
echo ========================================
echo.

cd /d "%~dp0"

echo [1/5] .vs 폴더 삭제 중...
if exist ".vs" (
    rd /s /q ".vs"
    echo   - 완료
) else (
    echo   - 폴더 없음
)

echo [2/5] LicenseManagerNet8 빌드 폴더 삭제 중...
if exist "LicenseManagerNet8\Debug" (
    rd /s /q "LicenseManagerNet8\Debug"
    echo   - Debug 폴더 삭제 완료
) else (
    echo   - Debug 폴더 없음
)

if exist "LicenseManagerNet8\Release" (
    rd /s /q "LicenseManagerNet8\Release"
    echo   - Release 폴더 삭제 완료
) else (
    echo   - Release 폴더 없음
)

if exist "LicenseManagerNet8\x64" (
    rd /s /q "LicenseManagerNet8\x64"
    echo   - x64 폴더 삭제 완료
) else (
    echo   - x64 폴더 없음
)

echo [3/5] TestApp 빌드 폴더 삭제 중...
if exist "TestApp\bin" (
    rd /s /q "TestApp\bin"
    echo   - bin 폴더 삭제 완료
) else (
    echo   - bin 폴더 없음
)

if exist "TestApp\obj" (
    rd /s /q "TestApp\obj"
    echo   - obj 폴더 삭제 완료
) else (
    echo   - obj 폴더 없음
)

echo [4/5] 루트 빌드 폴더 삭제 중...
if exist "Debug" (
    rd /s /q "Debug"
    echo   - Debug 폴더 삭제 완료
) else (
    echo   - Debug 폴더 없음
)

if exist "Release" (
    rd /s /q "Release"
    echo   - Release 폴더 삭제 완료
) else (
    echo   - Release 폴더 없음
)

if exist "x64" (
    rd /s /q "x64"
    echo   - x64 폴더 삭제 완료
) else (
    echo   - x64 폴더 없음
)

echo [5/5] NuGet 패키지 캐시 정리 중...
dotnet nuget locals all --clear >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo   - 완료
) else (
    echo   - 건너뜀 (dotnet CLI 없음)
)

echo.
echo ========================================
echo 정리 완료!
echo ========================================
echo.
echo 이제 Visual Studio에서 솔루션을 다시 빌드하세요.
echo.
pause
