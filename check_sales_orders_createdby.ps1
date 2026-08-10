$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

$cmd = $connection.CreateCommand()
$cmd.CommandText = @"
SELECT TOP 20 Id, OrderNumber, OrderDate, Status, TotalAmount, CreatedBy, CustomerId
FROM SalesOrders
ORDER BY CreatedOnUtc DESC;
"@

$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "Order:" $reader["OrderNumber"] "| Date:" $reader["OrderDate"] "| Status:" $reader["Status"] "| CreatedBy:" $reader["CreatedBy"] "| CustomerId:" $reader["CustomerId"]
}
$connection.Close()
