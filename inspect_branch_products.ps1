$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    bp.Id,
    p.InternalCode,
    p.Name AS ProductName,
    bp.Price,
    bp.Cost,
    bp.IsActive,
    bp.IsDeleted
FROM BranchProducts bp
JOIN Products p ON bp.ProductId = p.Id
WHERE p.InternalCode IN ('TA007', 'TA008', 'TA011', 'TA012')
ORDER BY p.InternalCode;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sqlScript
$reader = $command.ExecuteReader()

while ($reader.Read()) {
    Write-Host "Code:" $reader["InternalCode"] "| Name:" $reader["ProductName"] "| Price:" $reader["Price"] "| Cost:" $reader["Cost"] "| Active:" $reader["IsActive"]
}
$connection.Close()
