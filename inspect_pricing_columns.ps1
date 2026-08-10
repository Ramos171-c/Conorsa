$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Products', 'ProductPresentations', 'BranchProducts', 'PricingThresholds')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sqlScript
$reader = $command.ExecuteReader()

while ($reader.Read()) {
    Write-Host "[" $reader["TABLE_NAME"] "]" $reader["COLUMN_NAME"] "(" $reader["DATA_TYPE"] ")"
}
$connection.Close()
