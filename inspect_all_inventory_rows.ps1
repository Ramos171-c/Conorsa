$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    i.[Id] AS InventoryId,
    i.[ProductId],
    p.[InternalCode],
    p.[Name] AS ProductName,
    i.[BranchWarehouseId],
    i.[PhysicalStock],
    i.[ReservedStock],
    i.[CommittedStock],
    i.[IsDeleted]
FROM [Inventory] i
JOIN [Products] p ON i.[ProductId] = p.[Id]
WHERE p.[Name] LIKE '%MIDDAY%' OR p.[InternalCode] LIKE '%11%' OR p.[Name] LIKE '%PAÑAL%'
ORDER BY p.[InternalCode];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ ALL INVENTORY ROWS FOR DIAPERS ================"
    while ($reader.Read()) {
        Write-Host "InvId: "$reader["InventoryId"] "| Code: "$reader["InternalCode"] "| Stock: "$reader["PhysicalStock"] "| Deleted: "$reader["IsDeleted"] "| Name: "$reader["ProductName"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
