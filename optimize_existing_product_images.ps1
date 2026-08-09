param(
    [string]$TargetFolder = "EnterpriseBillingSystem.WebApi\wwwroot\uploads\products",
    [int]$MaxWidth = 1200,
    [int]$MaxHeight = 1200,
    [int]$Quality = 85
)

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $TargetFolder)) {
    Write-Host "Directorio no encontrado: $TargetFolder" -ForegroundColor Red
    exit 1
}

$files = Get-ChildItem -Path $TargetFolder -Recurse -File | Where-Object { $_.Extension -in '.jpg', '.jpeg', '.png', '.webp' }

Write-Host "Procesando $($files.Count) imágenes en $TargetFolder..." -ForegroundColor Cyan

$count = 0
$totalSavedBytes = 0

foreach ($file in $files) {
    try {
        $originalSize = $file.Length
        if ($originalSize -lt 200KB) {
            continue # Ya es bastante ligera
        }

        $img = [System.Drawing.Image]::FromFile($file.FullName)
        $w = $img.Width
        $h = $img.Height

        if ($w -gt $MaxWidth -or $h -gt $MaxHeight -or $originalSize -gt 500KB) {
            $scale = [Math]::Min($MaxWidth / $w, $MaxHeight / $h)
            if ($scale -gt 1.0) { $scale = 1.0 }

            $newW = [Math]::Max(1, [int]($w * $scale))
            $newH = [Math]::Max(1, [int]($h * $scale))

            $bmp = New-Object System.Drawing.Bitmap($newW, $newH)
            $graph = [System.Drawing.Graphics]::FromImage($bmp)
            $graph.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graph.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graph.DrawImage($img, 0, 0, $newW, $newH)

            $img.Dispose()

            $tempPath = "$($file.FullName).tmp"
            if ($file.Extension -eq '.png') {
                $bmp.Save($tempPath, [System.Drawing.Imaging.ImageFormat]::Png)
            } else {
                $codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq "image/jpeg" }
                $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
                $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, [int64]$Quality)
                $bmp.Save($tempPath, $codec, $encoderParams)
            }

            $bmp.Dispose()
            $graph.Dispose()

            $newSize = (Get-Item $tempPath).Length
            if ($newSize -lt $originalSize) {
                Remove-Item $file.FullName -Force
                Move-Item $tempPath $file.FullName -Force
                $saved = $originalSize - $newSize
                $totalSavedBytes += $saved
                $count++
                Write-Host "Optimizada: $($file.Name) ($([math]::Round($originalSize/1KB))KB -> $([math]::Round($newSize/1KB))KB)" -ForegroundColor Green
            } else {
                Remove-Item $tempPath -Force
            }
        } else {
            $img.Dispose()
        }
    } catch {
        Write-Host "Error al procesar $($file.Name): $_" -ForegroundColor Yellow
    }
}

Write-Host "Optimización finalizada. Imágenes procesadas: $count. Espacio ahorrado: $([math]::Round($totalSavedBytes/1MB, 2)) MB" -ForegroundColor Cyan
