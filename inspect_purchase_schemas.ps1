$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT TABLE_NAME, COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME IN ('PurchaseReceiptDetails', 'InventoryMovements', 'StockAdjustments')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ TABLES SCHEMAS ================"
    while ($reader.Read()) {
        Write-Host "Table: "$reader["TABLE_NAME"] "| Column: "$reader["COLUMN_NAME"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
