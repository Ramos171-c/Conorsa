try {
    $response = Invoke-RestMethod -Uri "http://167.99.13.177:8080/api/v1/auth/login" -Method Post -Body '{"username":"Ramos","password":"123"}' -ContentType "application/json"
    Write-Host "Respuesta Exito:" ($response | ConvertTo-Json)
} catch {
    Write-Host "Error en llamada:" $_.Exception.Message
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        Write-Host "Cuerpo de Error:" $body
    }
}
