$baseUrl = "http://167.99.13.177:8080/api/v1"
Write-Host "Iniciando autenticación en servidor remoto 167.99.13.177..."

$loginBody = @{
    username = "admin"
    password = "Admin@2026#Initial"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "SUCCESS: Autenticación remota exitosa. Token JWT obtenido."

    $headers = @{
        "Authorization" = "Bearer $token"
    }

    Write-Host "Ejecutando reinicio y depuración administrativa en el servidor remoto..."
    $resetResponse = Invoke-RestMethod -Uri "$baseUrl/sales-orders/admin-reset-and-cleanup" -Method Post -Headers $headers -ContentType "application/json"
    Write-Host "================ RESULTADO DE DEPURACIÓN EN SERVIDOR REMOTO ================"
    Write-Host $resetResponse.message
    Write-Host "=========================================================================="
}
catch {
    Write-Host "Error al intentar autenticar o depurar en servidor remoto :" $_.Exception.Message
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        Write-Host "Detalle HTTP :" $reader.ReadToEnd()
    }
}
