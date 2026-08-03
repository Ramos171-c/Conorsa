$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
DECLARE @Code NVARCHAR(50), @MasterId UNIQUEIDENTIFIER, @DuplicateId UNIQUEIDENTIFIER;

DECLARE dup_cursor CURSOR FOR
SELECT p1.InternalCode, p1.Id AS MasterId, p2.Id AS DuplicateId
FROM Products p1
JOIN Products p2 ON p1.InternalCode = p2.InternalCode AND p1.Id < p2.Id;

OPEN dup_cursor;
FETCH NEXT FROM dup_cursor INTO @Code, @MasterId, @DuplicateId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Reasignar SalesOrderDetails
    UPDATE SalesOrderDetails SET ProductId = @MasterId WHERE ProductId = @DuplicateId;
    -- Reasignar PurchaseReceiptDetails
    UPDATE PurchaseReceiptDetails SET ProductId = @MasterId WHERE ProductId = @DuplicateId;
    
    -- Manejar ProductPresentations de DuplicateId
    DELETE FROM ProductPriceHistories WHERE ProductPresentationId IN (SELECT Id FROM ProductPresentations WHERE ProductId = @DuplicateId);
    DELETE FROM InventoryMovementDetails WHERE ProductPresentationId IN (SELECT Id FROM ProductPresentations WHERE ProductId = @DuplicateId);
    
    -- Mover o borrar Inventory
    UPDATE Inventory SET ProductId = @MasterId WHERE ProductId = @DuplicateId AND NOT EXISTS (SELECT 1 FROM Inventory WHERE ProductId = @MasterId);
    DELETE FROM Inventory WHERE ProductId = @DuplicateId;
    
    -- Borrar presentaciones y producto duplicado
    DELETE FROM ProductPresentations WHERE ProductId = @DuplicateId;
    DELETE FROM Products WHERE Id = @DuplicateId;

    FETCH NEXT FROM dup_cursor INTO @Code, @MasterId, @DuplicateId;
END;

CLOSE dup_cursor;
DEALLOCATE dup_cursor;

SELECT InternalCode, Name FROM Products WHERE InternalCode LIKE 'TA%' ORDER BY InternalCode;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ DEDUPLICATED TA PRODUCTS ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Name: "$reader["Name"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

