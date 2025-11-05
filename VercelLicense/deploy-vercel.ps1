# ========================================
# Vercel 프로젝트 배포 스크립트
# ========================================

param(
    [switch]$SkipLogin,
    [switch]$Dev
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Vercel 프로젝트 배포" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# 프로젝트 디렉토리 확인
$projectPath = "C:\Users\junhoi\Desktop\Work\Acadv25JArch\VercelLicense"
if (-not (Test-Path $projectPath)) {
    Write-Host "✗ 프로젝트 경로를 찾을 수 없습니다: $projectPath" -ForegroundColor Red
    exit 1
}

Set-Location $projectPath
Write-Host "✓ 프로젝트 경로: $projectPath" -ForegroundColor Green

# 1. Vercel CLI 설치 확인
Write-Host "`n[1/5] Vercel CLI 확인..." -ForegroundColor Yellow
try {
    $vercelVersion = vercel --version 2>$null
    if ($vercelVersion) {
        Write-Host "✓ Vercel CLI 설치됨: $vercelVersion" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Vercel CLI가 설치되지 않았습니다" -ForegroundColor Red
    Write-Host "설치 명령: npm install -g vercel" -ForegroundColor Yellow
    exit 1
}

# 2. package.json 확인
Write-Host "`n[2/5] 의존성 확인..." -ForegroundColor Yellow
if (-not (Test-Path "package.json")) {
    Write-Host "✗ package.json을 찾을 수 없습니다" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "node_modules")) {
    Write-Host "! node_modules가 없습니다. 설치 중..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ npm install 실패" -ForegroundColor Red
        exit 1
    }
}
Write-Host "✓ 의존성 확인 완료" -ForegroundColor Green

# 3. Vercel 로그인 확인
if (-not $SkipLogin) {
    Write-Host "`n[3/5] Vercel 로그인 확인..." -ForegroundColor Yellow
    
    try {
        $whoami = vercel whoami 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ 로그인됨: $whoami" -ForegroundColor Green
        } else {
            Write-Host "! Vercel 로그인이 필요합니다" -ForegroundColor Yellow
            vercel login
            if ($LASTEXITCODE -ne 0) {
                Write-Host "✗ 로그인 실패" -ForegroundColor Red
                exit 1
            }
        }
    } catch {
        Write-Host "! Vercel 로그인 중..." -ForegroundColor Yellow
        vercel login
    }
} else {
    Write-Host "`n[3/5] 로그인 체크 건너뜀" -ForegroundColor Cyan
}

# 4. 환경 변수 확인
Write-Host "`n[4/5] 환경 변수 확인..." -ForegroundColor Yellow
if (Test-Path ".env.example") {
    Write-Host "✓ .env.example 파일 존재" -ForegroundColor Green
    Write-Host "! Vercel 대시보드에서 다음 변수를 설정하세요:" -ForegroundColor Yellow
    Write-Host "  - ENCRYPTION_KEY" -ForegroundColor Cyan
    Write-Host "  - ADMIN_KEY" -ForegroundColor Cyan
    Write-Host "  - POSTGRES_URL (Neon 연결 시 자동 생성)" -ForegroundColor Cyan
} else {
    Write-Host "! .env.example 파일이 없습니다" -ForegroundColor Yellow
}

# 5. 배포 실행
Write-Host "`n[5/5] 배포 시작..." -ForegroundColor Yellow

if ($Dev) {
    Write-Host "개발 모드로 실행..." -ForegroundColor Cyan
    vercel dev
} else {
    Write-Host "프로덕션 배포 중..." -ForegroundColor Cyan
    vercel --prod
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n========================================" -ForegroundColor Green
        Write-Host "✓ 배포 성공!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        
        # 배포 URL 저장
        $vercelUrl = vercel ls --json 2>$null | ConvertFrom-Json | Select-Object -First 1 -ExpandProperty url
        if ($vercelUrl) {
            $vercelUrl = "https://$vercelUrl"
            Write-Host "`n배포 URL: $vercelUrl" -ForegroundColor Cyan
            
            # URL을 파일에 저장
            $vercelUrl | Out-File -FilePath "deployment-url.txt" -Encoding UTF8
            Write-Host "URL이 deployment-url.txt에 저장되었습니다" -ForegroundColor Green
        }
        
        Write-Host "`n다음 단계:" -ForegroundColor Yellow
        Write-Host "1. Vercel 대시보드에서 Neon 데이터베이스 연결" -ForegroundColor Cyan
        Write-Host "2. 환경 변수 설정 (ENCRYPTION_KEY, ADMIN_KEY)" -ForegroundColor Cyan
        Write-Host "3. neon-db-query.ps1 -Init 로 DB 초기화" -ForegroundColor Cyan
        
    } else {
        Write-Host "`n✗ 배포 실패" -ForegroundColor Red
        exit 1
    }
}
