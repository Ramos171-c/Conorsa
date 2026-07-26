$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    w.[Id] AS WarehouseId,
    w.[Name] AS WarehouseName,
    SUM(ISNULL(i.[PhysicalStock], 0)) AS TotalStockInWarehouse
FROM [Warehouses] w
LEFT JOIN [BranchWarehouses] bw ON w.[Id] = bw.[WarehouseId]
LEFT JOIN [Inventory] i ON bw.[Id] = i.[BranchWarehouseId]
GROUP BY w.[Id], w.[Name];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ WAREHOUSES AND TOTAL STOCK ================"
    while ($reader.Read()) {
        Write-Host "Warehouse: "$reader["WarehouseName"] "| Total Stock: "$reader["TotalStockInWarehouse"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
