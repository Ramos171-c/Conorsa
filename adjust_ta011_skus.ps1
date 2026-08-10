$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

# 1. Update TA011 sizes (M, L, XL, XXL, XXXL, 4XL) to 50 PCS
$ta011Sizes = @("M", "L", "XL", "XXL", "XXXL", "4XL")

foreach ($size in $ta011Sizes) {
    $code = "TA011-$size"
    $name = "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA $size"
    $desc = "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA $size (50 PCS) (U/E: 1*4*50)"
    $shortDesc = "$name (50 PCS)"

    $cmd = $connection.CreateCommand()
    $cmd.CommandText = @"
UPDATE Products
SET Name = '$name',
    Description = '$desc',
    ShortDescription = '$shortDesc',
    LastModifiedBy = 'SystemAudit',
    LastModifiedOnUtc = GETUTCDATE()
WHERE InternalCode = '$code' AND IsDeleted = 0
"@
    $count = $cmd.ExecuteNonQuery()
    Write-Host "Updated [$code] to 50 PCS. Affected: $count"
}

# 2. Soft delete 5XL and 6XL
$toDelete = @("TA011-5XL", "TA011-6XL")
foreach ($code in $toDelete) {
    # Get Product Id
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = "SELECT Id FROM Products WHERE InternalCode = '$code'"
    $prodId = $cmd.ExecuteScalar()

    if ($null -ne $prodId) {
        $delProducts = $connection.CreateCommand()
        $delProducts.CommandText = "UPDATE Products SET IsDeleted = 1, IsActive = 0, LastModifiedBy = 'SystemAudit', LastModifiedOnUtc = GETUTCDATE() WHERE Id = '$prodId'"
        $delProducts.ExecuteNonQuery() | Out-Null

        $delPres = $connection.CreateCommand()
        $delPres.CommandText = "UPDATE ProductPresentations SET IsDeleted = 1, IsActive = 0 WHERE ProductId = '$prodId'"
        $delPres.ExecuteNonQuery() | Out-Null

        $delInv = $connection.CreateCommand()
        $delInv.CommandText = "UPDATE Inventory SET IsDeleted = 1 WHERE ProductId = '$prodId'"
        $delInv.ExecuteNonQuery() | Out-Null

        $delBp = $connection.CreateCommand()
        $delBp.CommandText = "UPDATE BranchProducts SET IsActive = 0 WHERE ProductId = '$prodId'"
        $delBp.ExecuteNonQuery() | Out-Null

        Write-Host "Deleted [$code] ($prodId) successfully from catalog and inventory."
    } else {
        Write-Host "SKU [$code] was not found."
    }
}

$connection.Close()
Write-Host "`n=== COMPLETED TA011 ADJUSTMENT ==="
