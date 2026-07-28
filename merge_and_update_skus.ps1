$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
-- Actualizar InternalCode de TO011, TO012, TO013 a TA011, TA012, TA013 donde no choque
UPDATE [Products] SET [InternalCode] = 'TA011' WHERE [InternalCode] = 'TO011' AND NOT EXISTS (SELECT 1 FROM [Products] WHERE [InternalCode] = 'TA011');
UPDATE [Products] SET [InternalCode] = 'TA012' WHERE [InternalCode] = 'TO012' AND NOT EXISTS (SELECT 1 FROM [Products] WHERE [InternalCode] = 'TA012');
UPDATE [Products] SET [InternalCode] = 'TA013' WHERE [InternalCode] = 'TO013' AND NOT EXISTS (SELECT 1 FROM [Products] WHERE [InternalCode] = 'TA013');

-- Para cualquier otro TO001..TO010 sin duplicado TA
UPDATE [Products] 
SET [InternalCode] = REPLACE([InternalCode], 'TO', 'TA') 
WHERE [InternalCode] LIKE 'TO%' 
  AND NOT EXISTS (
      SELECT 1 FROM [Products] p2 WHERE p2.[InternalCode] = REPLACE([Products].[InternalCode], 'TO', 'TA')
  );

SELECT [InternalCode], [Name] FROM [Products] WHERE [InternalCode] LIKE 'TA%' OR [InternalCode] LIKE 'TO%' ORDER BY [InternalCode];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ TA / TO SKUS ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Name: "$reader["Name"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
