$servers = @("167.99.13.177,1433", "(localdb)\MSSQLLocalDB")

$sqlScript = @"
SELECT 
    p.[InternalCode],
    p.[Name] AS ProductName,
    w.[Name] AS WarehouseName,
    i.[PhysicalStock],
    i.[ReservedStock],
    i.[CommittedStock]
FROM [Inventory] i
INNER JOIN [Products] p ON i.[ProductId] = p.[Id]
INNER JOIN [BranchWarehouses] bw ON i.[BranchWarehouseId] = bw.[Id]
INNER JOIN [Warehouses] w ON bw.[WarehouseId] = w.[Id]
WHERE p.[InternalCode] = 'TO012';
"@

foreach ($server in $servers) {
    Write-Host "================ INVENTARIO DE TO012 EN $server ================"
    $connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"
    if ($server.Contains("localdb")) {
        $connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;Integrated Security=True;TrustServerCertificate=True;"
    }

    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $sqlScript
        $reader = $command.ExecuteReader()
        
        while ($reader.Read()) {
            Write-Host "Producto :" $reader["InternalCode"] "-" $reader["ProductName"]
            Write-Host "Bodega   :" $reader["WarehouseName"]
            Write-Host "Físico   :" $reader["PhysicalStock"]
            Write-Host "Reservado:" $reader["ReservedStock"]
            Write-Host "--------------------------------------------------------"
        }
        $connection.Close()
    }
    catch {
        Write-Host "Error en $server :" $_.Exception.Message
    }
}
