# QuickBuild.ps1 - Windows 11 최적화 빌드 스크립트

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Windows 11 - LicenseManager 빌드 시작" -ForegroundColor Cyan
Write-Host "구성: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 프로젝트 디렉토리로 이동
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# 1. 정리
Write-Host "[1/5] 빌드 폴더 정리 중..." -ForegroundColor Yellow
Remove-Item -Path ".vs" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "x64" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "LicenseManagerNet8\x64" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "Debug" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "Release" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "TestApp\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "TestApp\obj" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  완료" -ForegroundColor Green

# 2. NuGet 캐시 정리
Write-Host "[2/5] NuGet 캐시 정리 중..." -ForegroundColor Yellow
dotnet nuget locals all --clear | Out-Null
Write-Host "  완료" -ForegroundColor Green

# 3. VS 개발 환경 로드
Write-Host "[3/5] Visual Studio 개발 환경 로드 중..." -ForegroundColor Yellow
$vsPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -products * -requires Microsoft.Component.MSBuild `
    -property installationPath

if ($vsPath) {
    $devShell = Join-Path $vsPath "Common7\Tools\Launch-VsDevShell.ps1"
    if (Test-Path $devShell) {
        & $devShell -Arch amd64 -HostArch amd64 | Out-Null
        Write-Host "  완료" -ForegroundColor Green
    } else {
        Write-Host "  경고: VS Dev Shell을 찾을 수 없습니다" -ForegroundColor Yellow
    }
} else {
    Write-Host "  경고: Visual Studio를 찾을 수 없습니다" -ForegroundColor Yellow
}

# 4. 빌드
Write-Host "[4/5] 솔루션 빌드 중..." -ForegroundColor Yellow
Write-Host "  빌드 명령 실행 중..." -ForegroundColor Gray

$msbuildArgs = @(
    "LicenseManagerNet8.sln",
    "/t:Rebuild",
    "/p:Configuration=$Configuration",
    "/p:Platform=x64",
    "/p:CLRSupport=NetCore",
    "/maxcpucount",
    "/verbosity:minimal"
)

$buildOutput = & msbuild $msbuildArgs 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "  빌드 성공!" -ForegroundColor Green
} else {
    Write-Host "  빌드 실패!" -ForegroundColor Red
    Write-Host ""
    Write-Host "빌드 오류 출력:" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Red
    exit 1
}

# 5. 결과 확인
Write-Host "[5/5] 빌드 결과 확인..." -ForegroundColor Yellow
$dllPath = "x64\$Configuration\LicenseManagerNet8.dll"
if (Test-Path $dllPath) {
    $dll = Get-Item $dllPath
    Write-Host "  DLL 생성: $($dll.FullName)" -ForegroundColor Green
    Write-Host "  파일 크기: $([math]::Round($dll.Length/1KB, 2)) KB" -ForegroundColor Green
    Write-Host "  생성 시간: $($dll.LastWriteTime)" -ForegroundColor Green
} else {
    Write-Host "  경고: DLL을 찾을 수 없습니다" -ForegroundColor Yellow
    Write-Host "  예상 경로: $dllPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "빌드 완료!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
