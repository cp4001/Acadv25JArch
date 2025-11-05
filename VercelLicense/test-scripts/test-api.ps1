# ========================================
# Vercel License Server 테스트 스크립트 (Neon)
# ========================================

# 설정
$API_BASE_URL = "https://your-project.vercel.app"  # ⚠️ 실제 URL로 변경
$ADMIN_KEY = "super-secret-admin-key-change-me-12345"  # ⚠️ 실제 키로 변경

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Vercel License Server 테스트 (Neon)" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# ========================================
# 0. 데이터베이스 초기화 (처음 1회만)
# ========================================
Write-Host "0. 데이터베이스 초기화..." -ForegroundColor Yellow

$initBody = @{
    adminKey = $ADMIN_KEY
} | ConvertTo-Json

try {
    $initResponse = Invoke-RestMethod -Uri "$API_BASE_URL/api/init-db" `
        -Method POST `
        -Body $initBody `
        -ContentType "application/json"
    
    Write-Host "✓ 데이터베이스 초기화 성공" -ForegroundColor Green
    Write-Host ($initResponse | ConvertTo-Json -Depth 3)
    Write-Host ""
}
catch {
    $errorMessage = $_.ErrorDetails.Message
    if ($errorMessage -like "*already exists*") {
        Write-Host "ℹ 테이블이 이미 존재합니다 (정상)" -ForegroundColor Cyan
    } else {
        Write-Host "✗ 초기화 실패: $_" -ForegroundColor Red
    }
    Write-Host ""
}

# ========================================
# 1. ID 등록 테스트
# ========================================
Write-Host "1. ID 등록 테스트..." -ForegroundColor Yellow

$registerBody = @{
    adminKey = $ADMIN_KEY
    id = "MACHINE-TEST-123"
    expiresAt = "2025-12-31"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$API_BASE_URL/api/register-id" `
        -Method POST `
        -Body $registerBody `
        -ContentType "application/json"
    
    Write-Host "✓ ID 등록 성공" -ForegroundColor Green
    Write-Host ($registerResponse | ConvertTo-Json -Depth 3)
    Write-Host ""
}
catch {
    Write-Host "✗ ID 등록 실패: $_" -ForegroundColor Red
    Write-Host ""
}

# ========================================
# 2. 라이선스 확인 테스트 (유효한 ID)
# ========================================
Write-Host "2. 라이선스 확인 테스트 (유효한 ID)..." -ForegroundColor Yellow

$checkBody = @{
    id = "MACHINE-TEST-123"
} | ConvertTo-Json

try {
    $checkResponse = Invoke-RestMethod -Uri "$API_BASE_URL/api/check-license" `
        -Method POST `
        -Body $checkBody `
        -ContentType "application/json"
    
    Write-Host "✓ 라이선스 확인 성공" -ForegroundColor Green
    Write-Host "  Valid: $($checkResponse.valid)" -ForegroundColor Green
    Write-Host "  Key: $($checkResponse.key)" -ForegroundColor Green
    Write-Host "  Expires: $($checkResponse.expiresAt)" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "✗ 라이선스 확인 실패: $_" -ForegroundColor Red
    Write-Host ""
}

# ========================================
# 3. 라이선스 확인 테스트 (잘못된 ID)
# ========================================
Write-Host "3. 라이선스 확인 테스트 (잘못된 ID)..." -ForegroundColor Yellow

$invalidCheckBody = @{
    id = "INVALID-ID-999"
} | ConvertTo-Json

try {
    $invalidResponse = Invoke-RestMethod -Uri "$API_BASE_URL/api/check-license" `
        -Method POST `
        -Body $invalidCheckBody `
        -ContentType "application/json"
    
    Write-Host "? 예상치 못한 성공 응답" -ForegroundColor Yellow
    Write-Host ($invalidResponse | ConvertTo-Json)
}
catch {
    $errorMessage = $_.ErrorDetails.Message | ConvertFrom-Json
    if ($errorMessage.error -like "*not found*") {
        Write-Host "✓ 올바르게 거부됨: $($errorMessage.error)" -ForegroundColor Green
    } else {
        Write-Host "✗ 예상치 못한 오류: $($errorMessage.error)" -ForegroundColor Red
    }
    Write-Host ""
}

# ========================================
# 4. 모든 ID 조회 테스트
# ========================================
Write-Host "4. 모든 ID 조회 테스트..." -ForegroundColor Yellow

$listBody = @{
    adminKey = $ADMIN_KEY
} | ConvertTo-Json

try {
    $listResponse = Invoke-RestMethod -Uri "$API_BASE_URL/api/list-ids" `
        -Method POST `
        -Body $listBody `
        -ContentType "application/json"
    
    Write-Host "✓ ID 목록 조회 성공" -ForegroundColor Green
    Write-Host "  총 개수: $($listResponse.count)" -ForegroundColor Green
    Write-Host "  목록:" -ForegroundColor Green
    foreach ($license in $listResponse.licenses) {
        Write-Host "    - $($license.id) (만료: $($license.expires_at))" -ForegroundColor Cyan
    }
    Write-Host ""
}
catch {
    Write-Host "✗ ID 목록 조회 실패: $_" -ForegroundColor Red
    Write-Host ""
}

# ========================================
# 5. ID 삭제 테스트
# ========================================
Write-Host "5. ID 삭제 테스트..." -ForegroundColor Yellow

$deleteBody = @{
    adminKey = $ADMIN_KEY
    id = "MACHINE-TEST-123"
} | ConvertTo-Json

try {
    $deleteResponse = Invoke-RestMethod -Uri "$API_BASE_URL/api/delete-id" `
        -Method POST `
        -Body $deleteBody `
        -ContentType "application/json"
    
    Write-Host "✓ ID 삭제 성공" -ForegroundColor Green
    Write-Host ($deleteResponse | ConvertTo-Json)
    Write-Host ""
}
catch {
    Write-Host "✗ ID 삭제 실패: $_" -ForegroundColor Red
    Write-Host ""
}

# ========================================
# 6. 삭제 후 라이선스 확인 (실패해야 함)
# ========================================
Write-Host "6. 삭제 후 라이선스 확인 (실패해야 함)..." -ForegroundColor Yellow

try {
    $finalCheckResponse = Invoke-RestMethod -Uri "$API_BASE_URL/api/check-license" `
        -Method POST `
        -Body $checkBody `
        -ContentType "application/json"
    
    Write-Host "? 예상치 못한 성공 응답" -ForegroundColor Yellow
}
catch {
    $errorMessage = $_.ErrorDetails.Message | ConvertFrom-Json
    if ($errorMessage.error -like "*not found*") {
        Write-Host "✓ 올바르게 거부됨 (ID가 삭제됨)" -ForegroundColor Green
    } else {
        Write-Host "✗ 예상치 못한 오류: $($errorMessage.error)" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "테스트 완료!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
