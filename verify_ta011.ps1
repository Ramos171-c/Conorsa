$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

$cmd = $connection.CreateCommand()
$cmd.CommandText = @"
SELECT InternalCode, Name, Description, IsDeleted
FROM Products
WHERE InternalCode LIKE 'TA011-%'
ORDER BY InternalCode;
"@

$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "Code:" $reader["InternalCode"] "| Name:" $reader["Name"] "| Desc:" $reader["Description"] "| Deleted:" $reader["IsDeleted"]
}
$connection.Close()
