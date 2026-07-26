$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.[Id],
    p.[InternalCode],
    p.[Name],
    p.[Description],
    i.[PhysicalStock]
FROM [Products] p
LEFT JOIN [Inventory] i ON p.[Id] = i.[ProductId]
WHERE p.[Name] LIKE '%MIDDAY%' 
   OR p.[Name] LIKE '%CALSON%' 
   OR p.[Name] LIKE '%PAÑAL%' 
   OR p.[Description] LIKE '%MIDDAY%'
ORDER BY p.[InternalCode];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ ALL DIAPER PRODUCTS WITH STOCK ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Stock: "$reader["PhysicalStock"] "| Name: "$reader["Name"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
