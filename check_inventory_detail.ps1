$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.[Id] AS ProductId,
    p.[InternalCode],
    p.[Name] AS ProductName,
    i.[PhysicalStock],
    i.[ReservedStock],
    i.[CommittedStock]
FROM [Products] p
LEFT JOIN [Inventory] i ON p.[Id] = i.[ProductId]
WHERE i.[PhysicalStock] > 0 OR p.[Name] LIKE '%MIDDAY%' OR p.[Name] LIKE '%PAÑAL%' OR p.[InternalCode] IN ('TO011', 'TA011', 'TO012', 'TA006')
ORDER BY p.[InternalCode];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ ALL PRODUCTS WITH STOCK OR SEARCHED ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Name: "$reader["ProductName"] "| Stock: "$reader["PhysicalStock"] "| Reserved: "$reader["ReservedStock"] "| Committed: "$reader["CommittedStock"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
