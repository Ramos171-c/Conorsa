
$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.Id AS ProductId,
    p.InternalCode,
    p.Name AS ProductName,
    p.Description,
    p.CurrentCost,
    p.MinimumStock,
    p.MaximumStock,
    p.ReorderPoint,
    p.DefaultUnitOfMeasureId,
    p.CategoryId,
    p.BrandId,
    p.TaxId,
    ISNULL(SUM(i.PhysicalStock), 0) AS CurrentStock,
    (
        SELECT STRING_AGG(CONCAT(pr.Id, '::', pr.Name, '::', pr.ConversionFactor, '::', pr.RetailPrice, '::', pr.WholesalePrice, '::', pr.SpecialPrice, '::', pr.Cost, '::', pr.IsBaseUnit, '::', pr.UnitOfMeasureId), ' || ')
        FROM ProductPresentations pr
        WHERE pr.ProductId = p.Id AND pr.IsDeleted = 0
    ) AS PresentationsStr
FROM Products p
LEFT JOIN Inventory i ON p.Id = i.ProductId AND i.IsDeleted = 0
WHERE p.IsDeleted = 0 AND (p.Name LIKE '%PAÑAL%' OR p.Name LIKE '%CALS%' OR p.Name LIKE '%OSITO%' OR p.Name LIKE '%LUCAS%' OR p.Name LIKE '%PEGA%')
GROUP BY p.Id, p.InternalCode, p.Name, p.Description, p.CurrentCost, p.MinimumStock, p.MaximumStock, p.ReorderPoint, p.DefaultUnitOfMeasureId, p.CategoryId, p.BrandId, p.TaxId
ORDER BY p.InternalCode;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sqlScript
$reader = $command.ExecuteReader()

$items = @()
while ($reader.Read()) {
    $items += [PSCustomObject]@{
        Id = $reader["ProductId"]
        InternalCode = $reader["InternalCode"]
        ProductName = $reader["ProductName"]
        Description = $reader["Description"]
        CurrentCost = $reader["CurrentCost"]
        DefaultUnitOfMeasureId = $reader["DefaultUnitOfMeasureId"]
        CategoryId = $reader["CategoryId"]
        BrandId = $reader["BrandId"]
        TaxId = $reader["TaxId"]
        CurrentStock = $reader["CurrentStock"]
        Presentations = $reader["PresentationsStr"]
    }
}
$connection.Close()

$items | ConvertTo-Json -Depth 5
