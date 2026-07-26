$baseUrl = "http://167.99.13.177:8080/api/v1"
Write-Host "Ejecutando reinicio y depuración administrativa en el servidor remoto 167.99.13.177..."

try {
    $resetResponse = Invoke-RestMethod -Uri "$baseUrl/sales-orders/admin-reset-and-cleanup" -Method Post -ContentType "application/json"
    Write-Host "================ RESULTADO DE DEPURACIÓN EN SERVIDOR REMOTO ================"
    Write-Host $resetResponse.message
    Write-Host "=========================================================================="
}
catch {
    Write-Host "Error al intentar depurar en servidor remoto :" $_.Exception.Message
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        Write-Host "Detalle HTTP :" $reader.ReadToEnd()
    }
}
