$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.[Name] AS ProductName,
    COUNT(p.[Id]) AS ProductCount,
    SUM(ISNULL(i.[PhysicalStock], 0)) AS TotalPhysicalStock
FROM [Products] p
LEFT JOIN [Inventory] i ON p.[Id] = i.[ProductId]
GROUP BY p.[Name]
HAVING COUNT(p.[Id]) > 1 OR SUM(ISNULL(i.[PhysicalStock], 0)) > 0
ORDER BY TotalPhysicalStock DESC;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ DUPLICATE PRODUCTS & COMBINED STOCK ================"
    while ($reader.Read()) {
        Write-Host "Name: "$reader["ProductName"] "| Count: "$reader["ProductCount"] "| Combined Stock: "$reader["TotalPhysicalStock"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
