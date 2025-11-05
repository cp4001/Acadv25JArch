@echo off
echo ========================================
echo DISPOSE 오류 완전 해결 스크립트
echo ========================================
echo.
echo 이 스크립트는 다음을 수행합니다:
echo 1. Visual Studio 프로세스 종료
echo 2. 모든 빌드 캐시 삭제
echo 3. NuGet 캐시 정리
echo 4. 완전히 새로운 빌드
echo.
pause

cd /d "%~dp0"

echo.
echo [1/6] Visual Studio 프로세스 종료 중...
taskkill /F /IM devenv.exe 2>nul
timeout /t 2 >nul

echo [2/6] LicenseAdminApp 빌드 캐시 삭제 중...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"

echo [3/6] 상위 폴더로 이동하여 전체 정리...
cd ..
if exist ".vs" rmdir /s /q ".vs"

echo [4/6] 모든 프로젝트 빌드 캐시 삭제 중...
for /d %%D in (*) do (
    if exist "%%D\bin" rmdir /s /q "%%D\bin"
    if exist "%%D\obj" rmdir /s /q "%%D\obj"
)

echo [5/6] NuGet 캐시 정리 및 복원 중...
dotnet nuget locals all --clear
dotnet restore Eleclicense.sln

echo [6/6] 솔루션 빌드 중...
dotnet build Eleclicense.sln --no-incremental

echo.
echo ========================================
echo 완료!
echo.
echo 다음 단계:
echo 1. Visual Studio를 다시 시작하세요
echo 2. Eleclicense.sln을 여세요
echo 3. Ctrl+Shift+B로 빌드하세요
echo 4. F5로 실행하세요
echo ========================================
echo.
pause
