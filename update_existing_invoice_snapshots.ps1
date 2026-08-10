$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

$cmd = $connection.CreateCommand()
$cmd.CommandText = @"
UPDATE sid
SET sid.ProductNameSnapshot = p.Description
FROM SalesInvoiceDetails sid
INNER JOIN Products p ON sid.ProductId = p.Id
WHERE p.Description IS NOT NULL AND p.Description <> '';
"@

$rows = $cmd.ExecuteNonQuery()
$connection.Close()

Write-Host "Updated $rows invoice line detail snapshots to use product Description (showing U/E)."
