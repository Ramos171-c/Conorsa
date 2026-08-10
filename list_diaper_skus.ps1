$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

$cmd = $connection.CreateCommand()
$cmd.CommandText = @"
SELECT Id, InternalCode, Name, Description
FROM Products
WHERE InternalCode LIKE 'TA007%' OR InternalCode LIKE 'TA008%' OR InternalCode LIKE 'TA011%' OR InternalCode LIKE 'TA012%'
ORDER BY InternalCode;
"@

$reader = $cmd.ExecuteReader()
$items = @()
while ($reader.Read()) {
    $items += [PSCustomObject]@{
        Id = $reader["Id"]
        Code = $reader["InternalCode"]
        Name = $reader["Name"]
        Desc = $reader["Description"]
    }
}
$connection.Close()

$items | ConvertTo-Json -Depth 5
