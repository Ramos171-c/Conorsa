$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Inventory';
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ INVENTORY SCHEMA ================"
    while ($reader.Read()) {
        Write-Host "Column: "$reader["COLUMN_NAME"] " ("$reader["DATA_TYPE"]")"
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
