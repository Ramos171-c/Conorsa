$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.InternalCode,
    p.Name AS ProductName,
    p.CurrentCost,
    bp.LocalSalePrice,
    bp.MinSalePrice,
    bp.MaxDiscountPercentage,
    pr.Name AS PresentationName,
    pr.ConversionFactor,
    pr.Cost AS PresCost,
    pr.RetailPrice,
    pr.SemiWholesalePrice,
    pr.WholesalePrice
FROM Products p
LEFT JOIN BranchProducts bp ON p.Id = bp.ProductId
LEFT JOIN ProductPresentations pr ON p.Id = pr.ProductId AND pr.IsDeleted = 0
WHERE p.InternalCode IN ('TA007', 'TA008', 'TA011', 'TA012') AND p.IsDeleted = 0
ORDER BY p.InternalCode;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sqlScript
$reader = $command.ExecuteReader()

while ($reader.Read()) {
    Write-Host "Code:" $reader["InternalCode"] "| Name:" $reader["ProductName"] "| Cost:" $reader["CurrentCost"] "| LocalPrice:" $reader["LocalSalePrice"] "| Pres:" $reader["PresentationName"] "| Retail:" $reader["RetailPrice"] "| SemiWholesale:" $reader["SemiWholesalePrice"] "| Wholesale:" $reader["WholesalePrice"]
}
$connection.Close()
