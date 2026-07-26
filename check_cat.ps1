$connectionString = "Server=167.99.13.177,1433;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='SupplierCategories'"
$reader = $command.ExecuteReader()
while ($reader.Read()) {
    Write-Host $reader[0]
}
$connection.Close()
