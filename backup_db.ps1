$backupPath = "C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystemDb_Backup_20260726.bak"
$servers = @("(localdb)\MSSQLLocalDB", ".", "localhost", "127.0.0.1")

foreach ($server in $servers) {
    Write-Host "Intentando respaldo en servidor $server..."
    $connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;Integrated Security=True;TrustServerCertificate=True;"
    $query = "BACKUP DATABASE [EnterpriseBillingSystemDb] TO DISK = N'$backupPath' WITH FORMAT, INIT, NAME = N'EnterpriseBillingSystemDb-Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $query
        $command.CommandTimeout = 300
        $command.ExecuteNonQuery()
        $connection.Close()
        Write-Host "SUCCESS: Respaldo de la base de datos creado exitosamente en $backupPath usando $server"
        break
    }
    catch {
        Write-Host "Error en $server :" $_.Exception.Message
    }
}
