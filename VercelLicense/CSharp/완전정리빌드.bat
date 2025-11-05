@echo off
echo ========================================
echo CS0115 오류 완전 제거 스크립트
echo ========================================
echo.
echo 이 스크립트는:
echo 1. Visual Studio 프로세스 강제 종료
echo 2. 모든 캐시 삭제 (.vs, bin, obj)
echo 3. NuGet 캐시 정리
echo 4. 완전히 새로운 빌드
echo.
echo 계속하려면 아무 키나 누르세요...
pause
echo.

:: 현재 위치를 CSharp 폴더로 이동
cd /d "%~dp0.."

echo [1/7] Visual Studio 프로세스 강제 종료...
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM MSBuild.exe 2>nul
timeout /t 3 >nul

echo [2/7] .vs 폴더 삭제...
if exist ".vs" (
    rmdir /s /q ".vs"
    echo    .vs 폴더 삭제 완료
) else (
    echo    .vs 폴더 없음
)

echo [3/7] 모든 bin 폴더 삭제...
for /d /r . %%d in (bin) do @if exist "%%d" (
    echo    삭제 중: %%d
    rmdir /s /q "%%d" 2>nul
)

echo [4/7] 모든 obj 폴더 삭제...
for /d /r . %%d in (obj) do @if exist "%%d" (
    echo    삭제 중: %%d
    rmdir /s /q "%%d" 2>nul
)

echo [5/7] NuGet 캐시 정리...
dotnet nuget locals all --clear

echo [6/7] 패키지 복원...
dotnet restore Eleclicense.sln

echo [7/7] 솔루션 빌드...
dotnet clean Eleclicense.sln
dotnet build Eleclicense.sln --no-incremental -v minimal

echo.
echo ========================================
echo 완료!
echo.
if %ERRORLEVEL% EQU 0 (
    echo ✅ 빌드 성공!
    echo.
    echo 다음 단계:
    echo 1. Visual Studio 2022를 실행하세요
    echo 2. Eleclicense.sln을 여세요
    echo 3. F5를 눌러 실행하세요
) else (
    echo ❌ 빌드 실패!
    echo 오류 메시지를 확인하세요.
)
echo ========================================
echo.
pause
