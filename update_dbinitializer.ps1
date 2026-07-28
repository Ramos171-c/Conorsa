$filePath = "C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.Infrastructure\Data\DbInitializer.cs"
$content = Get-Content $filePath -Raw

# 1. Asegurar que TO011, TO012, TO013 se llamen TA011, TA012, TA013 en DbInitializer.cs
$content = $content -replace '"TO011"', '"TA011"'
$content = $content -replace '"TO012"', '"TA012"'
$content = $content -replace '"TO013"', '"TA013"'

Set-Content -Path $filePath -Value $content -Encoding UTF8
Write-Host "DbInitializer.cs updated with TA011, TA012, TA013 SKUs."
