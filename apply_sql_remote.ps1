$databases = @("EnterpriseBillingSystemDb", "EnterpriseBillingSystemDb_LaUnion")

foreach ($dbName in $databases) {
    $connStr = "Server=167.99.13.177,1433;Database=${dbName};User Id=sa;Password=Perros..1;Encrypt=False;TrustServerCertificate=True;Connect Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

    try {
        Write-Host "Aplicando actualización SQL a ${dbName}..."
        $conn.Open()

        $sql = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'SellerCategory')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [SellerCategory] INT NOT NULL DEFAULT 0;
    PRINT 'SellerCategory agregada a Users';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductPresentations') AND name = 'AllowDetailChannel')
BEGIN
    ALTER TABLE [dbo].[ProductPresentations] ADD [AllowDetailChannel] BIT NOT NULL DEFAULT 1;
    PRINT 'AllowDetailChannel agregada a ProductPresentations';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductPresentations') AND name = 'AllowCostChannel')
BEGIN
    ALTER TABLE [dbo].[ProductPresentations] ADD [AllowCostChannel] BIT NOT NULL DEFAULT 1;
    PRINT 'AllowCostChannel agregada a ProductPresentations';
END
"@

        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        $cmd.ExecuteNonQuery()

        $checkCmd = $conn.CreateCommand()
        $checkCmd.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'SellerCategory'"
        $count = $checkCmd.ExecuteScalar()
        Write-Host "Resultado en ${dbName}: Columna SellerCategory en tabla Users = $count"

        $conn.Close()
    } catch {
        Write-Host "Error en ${dbName}:" $_.Exception.Message
    }
}
