@echo off
chcp 65001 >nul
echo ============================================
echo  JArchSbLicense.dll Build Script
echo ============================================
echo.

where cl >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] cl.exe not found.
    echo Run this script in the x64 Native Tools Command Prompt for VS.
    pause
    exit /b 1
)

echo [1/2] Compiling JArchSbLicense.dll ...
cl /nologo /utf-8 /LD /O2 /MD /EHsc /DJARCHSBLICENSE_EXPORTS JArchSbLicense.cpp ^
   /Fe:JArchSbLicense.dll /link /DEF:JArchSbLicense.def
if %errorlevel% neq 0 goto failed

echo.
echo [2/2] Compiling test_client.exe ...
cl /nologo /utf-8 /O2 /MD /EHsc test_client.cpp /Fe:test_client.exe /link JArchSbLicense.lib
if %errorlevel% neq 0 goto failed

del /q *.obj *.exp 2>nul
echo.
echo [SUCCESS] JArchSbLicense.dll / test_client.exe build complete!
echo           run "test_client.exe" to verify against the server.
exit /b 0

:failed
echo.
echo [FAILED] Build error occurred.
exit /b 1
