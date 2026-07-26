$backupPath = "C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystemDb_Backup_20260726.bak"
$server = "(localdb)\MSSQLLocalDB"
$connectionString = "Server=$server;Database=master;Integrated Security=True;TrustServerCertificate=True;"

Write-Host "Restaurando base de datos EnterpriseBillingSystemDb desde $backupPath..."

$query = @"
ALTER DATABASE [EnterpriseBillingSystemDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [EnterpriseBillingSystemDb] FROM DISK = N'$backupPath' WITH REPLACE;
ALTER DATABASE [EnterpriseBillingSystemDb] SET MULTI_USER;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $query
    $command.CommandTimeout = 300
    $command.ExecuteNonQuery()
    $connection.Close()
    Write-Host "SUCCESS: Base de datos restaurada exitosamente desde $backupPath"
}
catch {
    Write-Host "Error al restaurar base de datos :" $_.Exception.Message
}
