@echo off
chcp 65001 >nul
echo ============================================
echo  JArchLicense.dll Build Script
echo ============================================
echo.

where cl >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] cl.exe not found.
    echo Run this script in Visual Studio Developer Command Prompt.
    pause
    exit /b 1
)

echo Compiling...
cl /LD /O2 /DJARCHLICENSE_EXPORTS /EHsc JArchLicense.cpp /Fe:JArchLicense.dll /link /DEF:JArchLicense.def user32.lib

if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS] JArchLicense.dll build complete!
    del /q *.obj *.exp 2>nul
) else (
    echo.
    echo [FAILED] Build error occurred.
)

pause
