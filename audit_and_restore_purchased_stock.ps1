$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.InternalCode,
    p.Name,
    ISNULL(i.PhysicalStock, 0) AS CurrentPhysicalStock,
    SUM(prd.Quantity * ISNULL(pr.ConversionFactor, 1.0)) AS TotalBaseUnitsPurchased,
    (SUM(prd.Quantity * ISNULL(pr.ConversionFactor, 1.0)) * ISNULL(basePr.Cost, p.CurrentCost)) AS PurchasedStockValue
FROM PurchaseReceiptDetails prd
JOIN Products p ON prd.ProductId = p.Id
LEFT JOIN ProductPresentations pr ON p.Id = pr.ProductId AND prd.UnitOfMeasureId = pr.UnitOfMeasureId
LEFT JOIN ProductPresentations basePr ON p.Id = basePr.ProductId AND basePr.IsBaseUnit = 1
LEFT JOIN Inventory i ON p.Id = i.ProductId
GROUP BY p.InternalCode, p.Name, i.PhysicalStock, basePr.Cost, p.CurrentCost
ORDER BY p.InternalCode;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ PURCHASED BASE UNITS VS CURRENT INVENTORY ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| CurrentStock: "$reader["CurrentPhysicalStock"] "| TotalPurchasedBaseUnits: "$reader["TotalBaseUnitsPurchased"] "| Value: "$reader["PurchasedStockValue"] "| Name: "$reader["Name"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

