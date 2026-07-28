$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    i.Id AS InventoryId,
    i.ProductId,
    i.PhysicalStock,
    i.IsDeleted,
    p.InternalCode,
    p.Name
FROM Inventory i
LEFT JOIN Products p ON i.ProductId = p.Id
WHERE i.PhysicalStock > 0;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ ALL INVENTORY ROWS WITH PHYSICAL STOCK > 0 ================"
    while ($reader.Read()) {
        Write-Host "InvId: "$reader["InventoryId"] "| ProdId: "$reader["ProductId"] "| Code: "$reader["InternalCode"] "| Stock: "$reader["PhysicalStock"] "| Deleted: "$reader["IsDeleted"] "| Name: "$reader["Name"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
