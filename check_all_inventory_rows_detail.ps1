$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.InternalCode,
    p.Name,
    i.PhysicalStock,
    i.ReservedStock,
    i.CommittedStock,
    pr.Cost AS BaseCost,
    (i.PhysicalStock * ISNULL(pr.Cost, 0)) AS Val
FROM Products p
LEFT JOIN Inventory i ON p.Id = i.ProductId
LEFT JOIN ProductPresentations pr ON p.Id = pr.ProductId AND pr.IsBaseUnit = 1
WHERE p.InternalCode LIKE 'TA%' OR p.InternalCode LIKE 'TO%'
ORDER BY p.InternalCode;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ ALL INVENTORY ROWS FOR TA/TO ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Stock: "$reader["PhysicalStock"] "| BaseCost: "$reader["BaseCost"] "| Val: "$reader["Val"] "| Name: "$reader["Name"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

