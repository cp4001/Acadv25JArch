@echo off
rem ===========================================================================
rem  JArch Xdata build (cmd.exe)
rem    1) JArchXData.arx    (C++ / ObjectARX 2025, Release x64)
rem    2) JArchXDataNet.dll (C# wrapper, Release)
rem    3) copy .arx into the DLL output folder (deploy side-by-side)
rem  Usage: double-click, or run build.bat in cmd
rem ===========================================================================
setlocal enableextensions
set "ROOT=%~dp0"
set "ARXPROJ=%ROOT%JArchXData\JArchXData.vcxproj"
set "NETPROJ=%ROOT%JArchXDataNet\JArchXDataNet.csproj"
set "ARXOUT=%ROOT%JArchXData\x64\Release\JArchXData.arx"
set "DLLDIR=%ROOT%JArchXDataNet\bin\Release\net8.0-windows"

rem --- find MSBuild via vswhere ---------------------------------------------
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo [ERROR] vswhere.exe not found: "%VSWHERE%"
    goto :fail
)

set "MSBUILD="
for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD (
    echo [ERROR] MSBuild.exe not found.
    goto :fail
)
echo MSBuild : %MSBUILD%
echo.

rem --- 1) ARX (C++) ----------------------------------------------------------
echo ============================================================
echo  [1/3] Build ARX  (Release, x64)
echo ============================================================
"%MSBUILD%" "%ARXPROJ%" /p:Configuration=Release /p:Platform=x64 /t:Rebuild /v:minimal /nologo
if errorlevel 1 (
    echo.
    echo [FAILED] ARX build error.
    goto :fail
)
echo.

rem --- 2) C# wrapper ---------------------------------------------------------
echo ============================================================
echo  [2/3] Build C# wrapper  (Release)
echo ============================================================
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet not found. Check .NET SDK install.
    goto :fail
)
dotnet build "%NETPROJ%" -c Release --nologo
if errorlevel 1 (
    echo.
    echo [FAILED] C# build error.
    goto :fail
)
echo.

rem --- 3) copy .arx next to the .dll ----------------------------------------
echo ============================================================
echo  [3/3] Copy ARX into DLL folder
echo ============================================================
if not exist "%ARXOUT%" (
    echo [ERROR] ARX not found: "%ARXOUT%"
    goto :fail
)
if not exist "%DLLDIR%\" (
    echo [ERROR] DLL folder not found: "%DLLDIR%"
    goto :fail
)
copy /Y "%ARXOUT%" "%DLLDIR%\" >nul
if errorlevel 1 (
    echo [FAILED] copy error.
    goto :fail
)
echo Copied: "%DLLDIR%\JArchXData.arx"

echo.
echo ============================================================
echo  BUILD OK
echo    ARX : %ARXOUT%
echo    DLL : %DLLDIR%\JArchXDataNet.dll
echo    ARX copied next to DLL for deployment.
echo ============================================================
echo  (Deploy .arx and .dll in the same folder; exclude .pdb)
if "%~1"=="" pause
endlocal
exit /b 0

:fail
echo.
if "%~1"=="" pause
endlocal
exit /b 1
