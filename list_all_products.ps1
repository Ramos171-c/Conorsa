$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.[Id],
    p.[InternalCode],
    p.[Name],
    p.[Description]
FROM [Products] p
ORDER BY p.[InternalCode];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ ALL PRODUCTS IN DATABASE ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Name: "$reader["Name"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

