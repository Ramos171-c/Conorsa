$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
-- 1. Ver valor total de inventario calculado como en GetDashboardKpisAsync
SELECT 
    b.Name AS BranchName,
    SUM(i.PhysicalStock * pr.Cost) AS CalculatedInventoryValue
FROM Inventory i
JOIN BranchWarehouses bw ON i.BranchWarehouseId = bw.Id
JOIN Branches b ON bw.BranchId = b.Id
JOIN ProductPresentations pr ON i.ProductId = pr.ProductId
WHERE pr.IsBaseUnit = 1 AND pr.IsDeleted = 0
GROUP BY b.Name;

-- 2. Ver todas las filas de inventario con existencias mayores a 0, con sus códigos, nombres y costos
SELECT 
    p.InternalCode,
    p.Name AS ProductName,
    i.PhysicalStock,
    pr.Cost AS BaseUnitCost,
    p.CurrentCost AS ProductCurrentCost,
    (i.PhysicalStock * ISNULL(pr.Cost, p.CurrentCost)) AS TotalValue
FROM Inventory i
JOIN Products p ON i.ProductId = p.Id
LEFT JOIN ProductPresentations pr ON p.Id = pr.ProductId AND pr.IsBaseUnit = 1
WHERE i.PhysicalStock > 0
ORDER BY (i.PhysicalStock * ISNULL(pr.Cost, p.CurrentCost)) DESC;

-- 3. Ver si existen filas en Inventory que no tienen Product o Presentation o cuyo PhysicalStock está en otro ProductId
SELECT 
    i.Id AS InventoryId,
    i.ProductId,
    p.InternalCode,
    p.Name,
    p.IsDeleted AS ProductDeleted,
    i.PhysicalStock
FROM Inventory i
LEFT JOIN Products p ON i.ProductId = p.Id
WHERE i.PhysicalStock > 0 AND (p.Id IS NULL OR p.IsDeleted = 1);
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ 1. CALCULATED INVENTORY VALUE BY BRANCH ================"
    while ($reader.Read()) {
        Write-Host "Branch: "$reader["BranchName"] "| Value: "$reader["CalculatedInventoryValue"]
    }
    
    $reader.NextResult()
    Write-Host "`n================ 2. ITEMS WITH PHYSICAL STOCK > 0 ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Stock: "$reader["PhysicalStock"] "| Cost: "$reader["BaseUnitCost"] "| TotalVal: "$reader["TotalValue"] "| Name: "$reader["ProductName"]
    }

    $reader.NextResult()
    Write-Host "`n================ 3. ORPHANED INVENTORY ROWS ================"
    while ($reader.Read()) {
        Write-Host "InvId: "$reader["InventoryId"] "| ProdId: "$reader["ProductId"] "| Code: "$reader["InternalCode"] "| Stock: "$reader["PhysicalStock"]
    }

    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
