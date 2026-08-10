
$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.Id,
    p.InternalCode,
    p.Name,
    p.Description,
    p.CurrentCost,
    p.DefaultUnitOfMeasureId,
    p.CategoryId,
    p.BrandId,
    p.TaxId,
    p.AutoMarkSoldOut,
    p.TrackInventory,
    p.ProductType
FROM Products p
WHERE p.InternalCode IN ('TA007', 'TA008', 'TA011', 'TA012') AND p.IsDeleted = 0
ORDER BY p.InternalCode;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sqlScript
$reader = $command.ExecuteReader()

$products = @()
while ($reader.Read()) {
    $products += [PSCustomObject]@{
        Id = $reader["Id"]
        InternalCode = $reader["InternalCode"]
        Name = $reader["Name"]
        Description = $reader["Description"]
        CurrentCost = $reader["CurrentCost"]
        DefaultUnitOfMeasureId = $reader["DefaultUnitOfMeasureId"]
        CategoryId = $reader["CategoryId"]
        BrandId = $reader["BrandId"]
        TaxId = $reader["TaxId"]
    }
}
$connection.Close()

foreach ($p in $products) {
    $pId = $p.Id
    $presCmd = $connection.CreateCommand()
    $connection.Open()
    $presCmd.CommandText = "SELECT Name, ConversionFactor, Cost, RetailPrice, WholesalePrice, SpecialPrice, IsBaseUnit, IsDefaultSalePresentation, UnitOfMeasureId, Barcode FROM ProductPresentations WHERE ProductId = '$pId' AND IsDeleted = 0"
    $presReader = $presCmd.ExecuteReader()
    $presentations = @()
    while ($presReader.Read()) {
        $presentations += [PSCustomObject]@{
            Name = $presReader["Name"]
            ConversionFactor = $presReader["ConversionFactor"]
            Cost = $presReader["Cost"]
            RetailPrice = $presReader["RetailPrice"]
            WholesalePrice = $presReader["WholesalePrice"]
            SpecialPrice = $presReader["SpecialPrice"]
            IsBaseUnit = $presReader["IsBaseUnit"]
            IsDefaultSalePresentation = $presReader["IsDefaultSalePresentation"]
            UnitOfMeasureId = $presReader["UnitOfMeasureId"]
            Barcode = $presReader["Barcode"]
        }
    }
    $connection.Close()
    $p | Add-Member -NotePropertyName "Presentations" -NotePropertyValue $presentations
}

$products | ConvertTo-Json -Depth 5
