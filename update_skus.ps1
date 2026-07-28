$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
-- 1. Actualizar SKUs de TO011, TO012, TO013 a TA011, TA012, TA013 (y variantes TO001..TO010 a TA001..TA010 si existen)
UPDATE [Products] SET [InternalCode] = 'TA011' WHERE [InternalCode] = 'TO011';
UPDATE [Products] SET [InternalCode] = 'TA012' WHERE [InternalCode] = 'TO012';
UPDATE [Products] SET [InternalCode] = 'TA013' WHERE [InternalCode] = 'TO013';

-- Si existen TO001 a TO010 que no estén duplicados con TA, actualizarlos o eliminar duplicados vacíos
DELETE FROM [Products] WHERE [InternalCode] LIKE 'TO%' AND [Id] NOT IN (SELECT DISTINCT [ProductId] FROM [SalesOrderDetails]);
UPDATE [Products] SET [InternalCode] = REPLACE([InternalCode], 'TO', 'TA') WHERE [InternalCode] LIKE 'TO%';

SELECT [InternalCode], [Name] FROM [Products] WHERE [InternalCode] LIKE 'TA%' ORDER BY [InternalCode];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ UPDATED TOWEL / DIAPER SKUS ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Name: "$reader["Name"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
