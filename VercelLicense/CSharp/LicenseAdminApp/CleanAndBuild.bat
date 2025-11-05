@echo off
echo ====================================
echo LicenseAdminApp 빌드 문제 해결
echo ====================================
echo.

cd /d "%~dp0"

echo 1. bin 및 obj 폴더 정리 중...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"

echo 2. Visual Studio 캐시 정리 중...
if exist ".vs" rmdir /s /q ".vs"

echo 3. NuGet 복원 중...
dotnet restore

echo 4. 프로젝트 빌드 중...
dotnet build

echo.
echo ====================================
echo 완료! 이제 Visual Studio에서 솔루션을 다시 열어주세요.
echo ====================================
pause
