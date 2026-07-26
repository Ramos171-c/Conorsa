$connectionString = "Server=(localdb)\MSSQLLocalDB;Database=EnterpriseBillingSystemDb;Integrated Security=True;TrustServerCertificate=True;"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"
$reader = $command.ExecuteReader()
while ($reader.Read()) {
    Write-Host ($reader[0] + "." + $reader[1])
}
$connection.Close()
