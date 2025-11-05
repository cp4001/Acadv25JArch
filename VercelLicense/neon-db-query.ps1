# ========================================
# Neon DB 조회 및 관리 스크립트
# ========================================

param(
    [string]$Url = "",
    [string]$AdminKey = "",
    [switch]$Init,
    [switch]$List,
    [switch]$Register,
    [switch]$Delete,
    [switch]$Check,
    [string]$Id = "",
    [string]$Product = "",
    [string]$Username = "",
    [string]$ExpiresAt = "2025-12-31"
)

# URL과 AdminKey 자동 로드
if (-not $Url -and (Test-Path "deployment-url.txt")) {
    $Url = (Get-Content "deployment-url.txt" -Raw).Trim()
    Write-Host "✓ URL 자동 로드: $Url" -ForegroundColor Green
}

if (-not $AdminKey) {
    # .env 파일에서 ADMIN_KEY 찾기 시도 (있을 경우)
    if (Test-Path ".env") {
        $envContent = Get-Content ".env" -Raw
        if ($envContent -match 'ADMIN_KEY=(.+)') {
            $AdminKey = $matches[1].Trim()
            Write-Host "✓ ADMIN_KEY 자동 로드" -ForegroundColor Green
        }
    }
}

# 필수 파라미터 검증
if (-not $Url) {
    Write-Host "✗ URL이 필요합니다. -Url 파라미터를 사용하거나 deployment-url.txt 파일을 생성하세요" -ForegroundColor Red
    Write-Host "사용 예: .\neon-db-query.ps1 -Url 'https://your-project.vercel.app' -Init" -ForegroundColor Yellow
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Neon DB 관리 스크립트" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "API URL: $Url`n" -ForegroundColor Cyan

# ========================================
# 1. 데이터베이스 초기화
# ========================================
if ($Init) {
    if (-not $AdminKey) {
        Write-Host "✗ -AdminKey 파라미터가 필요합니다" -ForegroundColor Red
        exit 1
    }

    Write-Host "[초기화] 데이터베이스 테이블 생성..." -ForegroundColor Yellow
    
    $body = @{
        adminKey = $AdminKey
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/init-db" `
            -Method POST `
            -Body $body `
            -ContentType "application/json"
        
        Write-Host "✓ 데이터베이스 초기화 성공" -ForegroundColor Green
        Write-Host ($response | ConvertTo-Json -Depth 3)
    }
    catch {
        $errorMessage = $_.ErrorDetails.Message
        if ($errorMessage -like "*already exists*") {
            Write-Host "ℹ 테이블이 이미 존재합니다" -ForegroundColor Cyan
        } else {
            Write-Host "✗ 초기화 실패: $errorMessage" -ForegroundColor Red
        }
    }
}

# ========================================
# 2. 모든 ID 조회
# ========================================
if ($List) {
    if (-not $AdminKey) {
        Write-Host "✗ -AdminKey 파라미터가 필요합니다" -ForegroundColor Red
        exit 1
    }

    Write-Host "[조회] 등록된 라이선스 목록..." -ForegroundColor Yellow
    
    $body = @{
        adminKey = $AdminKey
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/list-ids" `
            -Method POST `
            -Body $body `
            -ContentType "application/json"
        
        Write-Host "✓ 조회 성공 (총 $($response.count)개)" -ForegroundColor Green
        Write-Host ""
        
        if ($response.licenses.Count -gt 0) {
            Write-Host "등록된 라이선스:" -ForegroundColor Cyan
            foreach ($license in $response.licenses) {
                $status = if ($license.valid) { "✓ 유효" } else { "✗ 무효" }
                Write-Host "  [$status] ID: $($license.id)" -ForegroundColor $(if ($license.valid) { "Green" } else { "Red" })
                if ($license.product) {
                    Write-Host "      제품: $($license.product)" -ForegroundColor Cyan
                }
                if ($license.username) {
                    Write-Host "      사용자: $($license.username)" -ForegroundColor Cyan
                }
                Write-Host "      등록일: $($license.registered_at)" -ForegroundColor Gray
                Write-Host "      만료일: $($license.expires_at)" -ForegroundColor Gray
                Write-Host ""
            }
        } else {
            Write-Host "등록된 라이선스가 없습니다" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "✗ 조회 실패: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# ========================================
# 3. ID 등록
# ========================================
if ($Register) {
    if (-not $AdminKey) {
        Write-Host "✗ -AdminKey 파라미터가 필요합니다" -ForegroundColor Red
        exit 1
    }
    
    if (-not $Id) {
        Write-Host "✗ -Id 파라미터가 필요합니다" -ForegroundColor Red
        Write-Host "사용 예: .\neon-db-query.ps1 -Register -Id 'MACHINE-123' -Product 'MyApp' -Username 'John' -ExpiresAt '2025-12-31'" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "[등록] 새 라이선스 ID 등록..." -ForegroundColor Yellow
    Write-Host "  ID: $Id" -ForegroundColor Cyan
    if ($Product) {
        Write-Host "  제품: $Product" -ForegroundColor Cyan
    }
    if ($Username) {
        Write-Host "  사용자: $Username" -ForegroundColor Cyan
    }
    Write-Host "  만료일: $ExpiresAt" -ForegroundColor Cyan
    
    $bodyHash = @{
        adminKey = $AdminKey
        id = $Id
        expiresAt = $ExpiresAt
    }
    
    if ($Product) {
        $bodyHash.product = $Product
    }
    
    if ($Username) {
        $bodyHash.username = $Username
    }
    
    $body = $bodyHash | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/register-id" `
            -Method POST `
            -Body $body `
            -ContentType "application/json"
        
        Write-Host "✓ 등록 성공" -ForegroundColor Green
        Write-Host ($response | ConvertTo-Json -Depth 3)
    }
    catch {
        Write-Host "✗ 등록 실패: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# ========================================
# 4. ID 삭제
# ========================================
if ($Delete) {
    if (-not $AdminKey) {
        Write-Host "✗ -AdminKey 파라미터가 필요합니다" -ForegroundColor Red
        exit 1
    }
    
    if (-not $Id) {
        Write-Host "✗ -Id 파라미터가 필요합니다" -ForegroundColor Red
        Write-Host "사용 예: .\neon-db-query.ps1 -Delete -Id 'MACHINE-123'" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "[삭제] 라이선스 ID 삭제..." -ForegroundColor Yellow
    Write-Host "  ID: $Id" -ForegroundColor Cyan
    
    $body = @{
        adminKey = $AdminKey
        id = $Id
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/delete-id" `
            -Method POST `
            -Body $body `
            -ContentType "application/json"
        
        Write-Host "✓ 삭제 성공" -ForegroundColor Green
        Write-Host ($response | ConvertTo-Json -Depth 3)
    }
    catch {
        Write-Host "✗ 삭제 실패: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# ========================================
# 5. 라이선스 확인 (클라이언트용)
# ========================================
if ($Check) {
    if (-not $Id) {
        Write-Host "✗ -Id 파라미터가 필요합니다" -ForegroundColor Red
        Write-Host "사용 예: .\neon-db-query.ps1 -Check -Id 'MACHINE-123'" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "[확인] 라이선스 유효성 검증..." -ForegroundColor Yellow
    Write-Host "  ID: $Id" -ForegroundColor Cyan
    
    $body = @{
        id = $Id
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/check-license" `
            -Method POST `
            -Body $body `
            -ContentType "application/json"
        
        if ($response.valid) {
            Write-Host "✓ 유효한 라이선스" -ForegroundColor Green
            Write-Host "  암호화 키: $($response.key)" -ForegroundColor Cyan
            Write-Host "  만료일: $($response.expiresAt)" -ForegroundColor Cyan
            Write-Host "  등록일: $($response.registeredAt)" -ForegroundColor Cyan
        } else {
            Write-Host "✗ 무효한 라이선스" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "✗ 확인 실패: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# 사용법 표시
if (-not ($Init -or $List -or $Register -or $Delete -or $Check)) {
    Write-Host "사용법:" -ForegroundColor Yellow
    Write-Host "  데이터베이스 초기화:" -ForegroundColor Cyan
    Write-Host "    .\neon-db-query.ps1 -Init -AdminKey 'your-key'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  라이선스 목록 조회:" -ForegroundColor Cyan
    Write-Host "    .\neon-db-query.ps1 -List -AdminKey 'your-key'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  라이선스 등록:" -ForegroundColor Cyan
    Write-Host "    .\neon-db-query.ps1 -Register -Id 'MACHINE-123' -Product 'MyApp' -Username 'John' -ExpiresAt '2025-12-31' -AdminKey 'your-key'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  라이선스 삭제:" -ForegroundColor Cyan
    Write-Host "    .\neon-db-query.ps1 -Delete -Id 'MACHINE-123' -AdminKey 'your-key'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  라이선스 확인:" -ForegroundColor Cyan
    Write-Host "    .\neon-db-query.ps1 -Check -Id 'MACHINE-123'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "옵션:" -ForegroundColor Yellow
    Write-Host "  -Url : Vercel 배포 URL (deployment-url.txt에서 자동 로드)" -ForegroundColor Gray
    Write-Host "  -AdminKey : 관리자 키 (.env에서 자동 로드 시도)" -ForegroundColor Gray
    Write-Host "  -Product : 제품명 (선택사항)" -ForegroundColor Gray
    Write-Host "  -Username : 사용자명 (선택사항)" -ForegroundColor Gray
}
