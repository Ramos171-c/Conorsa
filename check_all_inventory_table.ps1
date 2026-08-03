$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.[InternalCode],
    p.[Name] AS ProductName,
    i.[PhysicalStock]
FROM [Inventory] i
JOIN [Products] p ON i.[ProductId] = p.[Id]
ORDER BY p.[InternalCode];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ EVERY PRODUCT IN INVENTORY TABLE ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Stock: "$reader["PhysicalStock"] "| Name: "$reader["ProductName"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

