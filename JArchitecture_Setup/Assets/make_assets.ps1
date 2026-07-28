# JArchitecture 설치 마법사 이미지/아이콘 생성
# 관례: C:\Users\junhoi\.claude\docs\inno_setup_conventions.md §1
# 실행: pwsh -File make_assets.ps1   (결과물은 이 스크립트와 같은 폴더)

Add-Type -AssemblyName System.Drawing

$OutDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# 브랜드 색 (다크 네이비 -> 틸)
$C1 = [System.Drawing.Color]::FromArgb(255, 16, 42, 74)
$C2 = [System.Drawing.Color]::FromArgb(255, 20, 132, 148)

function New-Badge {
    param([int]$W, [int]$H, [double]$MarkRatio, [string]$Caption)

    $bmp = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.InterpolationMode = 'HighQualityBicubic'

    # 대각선 그라데이션 배경
    $rect = New-Object System.Drawing.Rectangle(0, 0, $W, $H)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $C1, $C2, 45.0)
    $g.FillRectangle($brush, $rect)

    # 은은한 대각선 라인 (건축 제도 느낌)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(28, 255, 255, 255), [single]($W / 60.0))
    $step = [int]([Math]::Max(8, $W / 5))
    for ($x = -$H; $x -lt $W + $H; $x += $step) {
        $g.DrawLine($pen, $x, $H, ($x + $H), 0)
    }
    $pen.Dispose()

    # "JA" 마크
    $markSize = [single]([Math]::Min($W, $H) * $MarkRatio)
    $font = New-Object System.Drawing.Font('Segoe UI', $markSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = 'Center'
    $fmt.LineAlignment = 'Center'

    $markRect = if ($Caption) {
        New-Object System.Drawing.RectangleF(0, 0, [single]$W, [single]($H * 0.72))
    } else {
        New-Object System.Drawing.RectangleF(0, 0, [single]$W, [single]$H)
    }

    # 그림자 -> 본문
    $shadow = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70, 0, 0, 0))
    $shRect = New-Object System.Drawing.RectangleF(
        [single]($markRect.X + $W * 0.012), [single]($markRect.Y + $W * 0.012), $markRect.Width, $markRect.Height)
    $g.DrawString('JA', $font, $shadow, $shRect, $fmt)
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.DrawString('JA', $font, $white, $markRect, $fmt)

    if ($Caption) {
        $capSize = [single]([Math]::Max(9.0, $W * 0.088))
        $capFont = New-Object System.Drawing.Font('Segoe UI', $capSize, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $capBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
        $capRect = New-Object System.Drawing.RectangleF(0, [single]($H * 0.70), [single]$W, [single]($H * 0.16))
        $g.DrawString($Caption, $capFont, $capBrush, $capRect, $fmt)
        $capFont.Dispose(); $capBrush.Dispose()
    }

    $font.Dispose(); $white.Dispose(); $shadow.Dispose(); $brush.Dispose(); $g.Dispose()
    return $bmp
}

function Save-Png {
    param([System.Drawing.Bitmap]$Bmp, [string]$Path)
    $Bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host ("생성: {0}  ({1}x{2})" -f (Split-Path -Leaf $Path), $Bmp.Width, $Bmp.Height)
}

# --- 마법사 이미지 ---
$big = New-Badge -W 200 -H 380 -MarkRatio 0.46 -Caption 'JArchitecture'
Save-Png $big (Join-Path $OutDir 'WizardImage.png')
$big.Dispose()

$small = New-Badge -W 110 -H 116 -MarkRatio 0.52 -Caption $null
Save-Png $small (Join-Path $OutDir 'WizardSmallImage.png')
$small.Dispose()

# --- 멀티사이즈 .ico (PNG 압축 엔트리, Vista+ 지원) ---
$sizes = @(16, 32, 48, 256)
$pngs = @()
foreach ($s in $sizes) {
    $b = New-Badge -W $s -H $s -MarkRatio 0.58 -Caption $null
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += , $ms.ToArray()
    $ms.Dispose(); $b.Dispose()
}

$icoPath = Join-Path $OutDir 'JArchitecture.ico'
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
try {
    $bw.Write([UInt16]0)                # reserved
    $bw.Write([UInt16]1)                # type = icon
    $bw.Write([UInt16]$sizes.Count)     # count

    $offset = 6 + (16 * $sizes.Count)
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
        $bw.Write([Byte]$dim)           # width  (256 -> 0)
        $bw.Write([Byte]$dim)           # height
        $bw.Write([Byte]0)              # palette
        $bw.Write([Byte]0)              # reserved
        $bw.Write([UInt16]1)            # planes
        $bw.Write([UInt16]32)           # bit count
        $bw.Write([UInt32]$pngs[$i].Length)
        $bw.Write([UInt32]$offset)
        $offset += $pngs[$i].Length
    }
    foreach ($p in $pngs) { $bw.Write($p) }
}
finally {
    $bw.Dispose(); $fs.Dispose()
}
Write-Host ("생성: JArchitecture.ico  ({0} 사이즈)" -f ($sizes -join '/'))
