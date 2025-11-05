# License Database Query Script
# 사용법: .\query-licenses.ps1

$ErrorActionPreference = "Stop"

# ⚠️ 설정: 실제 값으로 변경하세요
$API_URL = "https://elec-license.vercel.app"
$ADMIN_KEY = "super-secret-admin-key-change-me-12345"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  License Database Console Viewer" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

try {
    Write-Host "Loading licenses..." -ForegroundColor Yellow

    # API 호출
    $body = @{
        adminKey = $ADMIN_KEY
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$API_URL/api/list-ids" `
        -Method Post `
        -Body $body `
        -ContentType "application/json"

    if ($response.success) {
        $licenses = $response.licenses
        $count = $licenses.Count

        Write-Host "`nTotal Licenses: $count" -ForegroundColor Green
        Write-Host ("-" * 120) -ForegroundColor Gray
        
        # 헤더
        $header = "{0,-40} {1,-10} {2,-20} {3,-20} {4,-20}" -f "Machine ID", "Valid", "Registered", "Expires", "Updated"
        Write-Host $header -ForegroundColor White
        Write-Host ("-" * 120) -ForegroundColor Gray

        # 데이터
        foreach ($license in $licenses) {
            $id = $license.id
            $valid = if ($license.valid) { "✓ Yes" } else { "✗ No" }
            $registered = if ($license.registeredAt) { 
                (Get-Date $license.registeredAt).ToString("yyyy-MM-dd HH:mm") 
            } else { "N/A" }
            $expires = if ($license.expiresAt) { 
                (Get-Date $license.expiresAt).ToString("yyyy-MM-dd") 
            } else { "Never" }
            $updated = if ($license.updatedAt) { 
                (Get-Date $license.updatedAt).ToString("yyyy-MM-dd HH:mm") 
            } else { "N/A" }

            $color = if ($license.valid) { "Green" } else { "Red" }
            $line = "{0,-40} {1,-10} {2,-20} {3,-20} {4,-20}" -f $id, $valid, $registered, $expires, $updated
            Write-Host $line -ForegroundColor $color
        }

        Write-Host ("-" * 120) -ForegroundColor Gray
        Write-Host "`n✓ Total: $count licenses" -ForegroundColor Green

        # 통계
        $validCount = ($licenses | Where-Object { $_.valid -eq $true }).Count
        $expiredCount = ($licenses | Where-Object { $_.valid -eq $false }).Count
        $noExpiryCount = ($licenses | Where-Object { $null -eq $_.expiresAt }).Count

        Write-Host "`n📊 Statistics:" -ForegroundColor Cyan
        Write-Host "   Valid:     $validCount" -ForegroundColor Green
        Write-Host "   Expired:   $expiredCount" -ForegroundColor Red
        Write-Host "   No Expiry: $noExpiryCount" -ForegroundColor Yellow

    } else {
        Write-Host "✗ Error: $($response.error)" -ForegroundColor Red
    }

} catch {
    Write-Host "`n✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
