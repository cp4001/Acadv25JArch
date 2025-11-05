@echo off
echo ====================================
echo 전체 솔루션 빌드 문제 해결
echo ====================================
echo.

cd /d "%~dp0"

echo 모든 프로젝트의 bin, obj, .vs 폴더를 정리합니다...
echo.

REM LicenseGenerator
echo [1/4] LicenseGenerator 정리 중...
cd LicenseGenerator
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
cd ..

REM LicenseCheckLibrary
echo [2/4] LicenseCheckLibrary 정리 중...
cd LicenseCheckLibrary
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
cd ..

REM TestApp
echo [3/4] TestApp 정리 중...
cd TestApp
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
cd ..

REM LicenseAdminApp
echo [4/4] LicenseAdminApp 정리 중...
cd LicenseAdminApp
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
cd ..

REM Visual Studio 캐시
echo.
echo Visual Studio 캐시 정리 중...
if exist ".vs" rmdir /s /q ".vs"

echo.
echo NuGet 패키지 복원 중...
dotnet restore Eleclicense.sln

echo.
echo 솔루션 빌드 중...
dotnet build Eleclicense.sln

echo.
echo ====================================
echo 완료!
echo 이제 Visual Studio를 다시 시작하고
echo Eleclicense.sln을 열어주세요.
echo ====================================
echo.
pause
