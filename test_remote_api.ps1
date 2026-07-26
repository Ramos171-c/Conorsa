$baseUrl = "http://167.99.13.177:8080/api/v1"

$loginBody = @{
    username = "admin"
    password = "Admin@2026#Initial"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "JWT Token Obtenido."

$headers = @{
    "Authorization" = "Bearer $token"
}

Write-Host "Consultando pedidos en el servidor remoto..."
$ordersResponse = Invoke-RestMethod -Uri "$baseUrl/sales-orders?pageNumber=1&pageSize=100" -Method Get -Headers $headers

Write-Host "Total Pedidos en Servidor Remoto:" $ordersResponse.totalCount
foreach ($item in $ordersResponse.items) {
    Write-Host "Pedido:" $item.orderNumber "Fecha:" $item.orderDate "Estado:" $item.status "Cliente:" $item.customerName
}
