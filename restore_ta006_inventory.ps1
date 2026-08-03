$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
DECLARE @ProductIdTA006 UNIQUEIDENTIFIER, @ProductIdTA010 UNIQUEIDENTIFIER, @BranchWarehouseId UNIQUEIDENTIFIER;

SELECT TOP 1 @ProductIdTA006 = Id FROM Products WHERE InternalCode = 'TA006';
SELECT TOP 1 @ProductIdTA010 = Id FROM Products WHERE InternalCode = 'TA010';
SELECT TOP 1 @BranchWarehouseId = Id FROM BranchWarehouses;

-- Actualizar o Insertar TA006
IF EXISTS (SELECT 1 FROM Inventory WHERE ProductId = @ProductIdTA006)
BEGIN
    UPDATE Inventory SET PhysicalStock = 307.00, IsDeleted = 0 WHERE ProductId = @ProductIdTA006;
END
ELSE
BEGIN
    INSERT INTO Inventory (Id, BranchWarehouseId, ProductId, PhysicalStock, ReservedStock, CommittedStock, IsDeleted)
    VALUES (NEWID(), @BranchWarehouseId, @ProductIdTA006, 307.00, 0, 0, 0);
END

-- Actualizar o Insertar TA010
IF EXISTS (SELECT 1 FROM Inventory WHERE ProductId = @ProductIdTA010)
BEGIN
    UPDATE Inventory SET PhysicalStock = 2.00, IsDeleted = 0 WHERE ProductId = @ProductIdTA010;
END
ELSE
BEGIN
    INSERT INTO Inventory (Id, BranchWarehouseId, ProductId, PhysicalStock, ReservedStock, CommittedStock, IsDeleted)
    VALUES (NEWID(), @BranchWarehouseId, @ProductIdTA010, 2.00, 0, 0, 0);
END

-- Recalcular valor total de inventario por sucursal
SELECT 
    b.Name AS BranchName,
    SUM(i.PhysicalStock * pr.Cost) AS CalculatedInventoryValue
FROM Inventory i
JOIN BranchWarehouses bw ON i.BranchWarehouseId = bw.Id
JOIN Branches b ON bw.BranchId = b.Id
JOIN ProductPresentations pr ON i.ProductId = pr.ProductId
WHERE pr.IsBaseUnit = 1 AND pr.IsDeleted = 0
GROUP BY b.Name;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ RESTORED INVENTORY VALUATION ================"
    while ($reader.Read()) {
        Write-Host "Branch: "$reader["BranchName"] "| Total Inventory Value: "$reader["CalculatedInventoryValue"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

