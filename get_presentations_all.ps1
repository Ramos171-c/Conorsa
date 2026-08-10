
$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    pr.Id,
    p.InternalCode,
    p.Name AS ProductName,
    pr.Name AS PresentationName,
    pr.ConversionFactor,
    pr.Cost,
    pr.RetailPrice,
    pr.WholesalePrice,
    pr.SpecialPrice,
    pr.IsBaseUnit,
    pr.IsDefaultSalePresentation,
    pr.IsDeleted
FROM ProductPresentations pr
JOIN Products p ON pr.ProductId = p.Id
WHERE p.InternalCode IN ('TA007', 'TA008', 'TA011', 'TA012')
ORDER BY p.InternalCode, pr.ConversionFactor;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sqlScript
$reader = $command.ExecuteReader()

while ($reader.Read()) {
    Write-Host "Code:" $reader["InternalCode"] "| Name:" $reader["PresentationName"] "| Cost:" $reader["Cost"] "| Retail:" $reader["RetailPrice"] "| Wholesale:" $reader["WholesalePrice"] "| Special:" $reader["SpecialPrice"] "| Factor:" $reader["ConversionFactor"] "| Deleted:" $reader["IsDeleted"]
}
$connection.Close()
