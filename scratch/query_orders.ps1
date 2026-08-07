$body = @{ username = "admin"; password = "Admin@2026#Initial" } | ConvertTo-Json
try {
    $res = Invoke-RestMethod -Uri "http://167.99.13.177:8080/api/v1/auth/login" -Method Post -Body $body -ContentType "application/json"
    $token = $res.token
    $headers = @{ Authorization = "Bearer $token" }

    $routes = Invoke-RestMethod -Uri "http://167.99.13.177:8080/api/v1/routes" -Headers $headers
    Write-Host "--- RUTAS ---"
    $routes | Select-Object id, name, code | Format-Table -AutoSize

    $ordersUrl = "http://167.99.13.177:8080/api/v1/sales-orders?fromDate=2026-07-20&toDate=2026-07-26T23:59:59&pageSize=500"
    $orders = Invoke-RestMethod -Uri $ordersUrl -Headers $headers
    Write-Host "--- PEDIDOS 20 AL 26 JULIO (Total: $($orders.totalCount)) ---"
    $orders.items | Select-Object id, orderNumber, customerName, status, totalAmount | Format-Table -AutoSize
} catch {
    Write-Host "Error: $_"
}
