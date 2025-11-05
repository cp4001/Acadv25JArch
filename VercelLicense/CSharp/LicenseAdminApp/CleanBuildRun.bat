@echo off
echo ========================================
echo License Admin App - 완전 클린 빌드
echo ========================================
echo.

cd /d "%~dp0"

echo [1/4] bin 폴더 삭제 중...
if exist bin rd /s /q bin
echo ✓ 완료

echo [2/4] obj 폴더 삭제 중...
if exist obj rd /s /q obj
echo ✓ 완료

echo [3/4] 빌드 중...
dotnet build --no-incremental
if errorlevel 1 (
    echo ✗ 빌드 실패!
    pause
    exit /b 1
)
echo ✓ 빌드 완료

echo [4/4] 실행 중...
echo.
echo ========================================
dotnet run
