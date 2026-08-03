$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
-- Check Inventory rows for TA006 or Papel Higienico in database
SELECT 
    i.Id AS InventoryId,
    i.ProductId,
    p.InternalCode,
    p.Name,
    i.PhysicalStock,
    i.IsDeleted
FROM Inventory i
JOIN Products p ON i.ProductId = p.Id
WHERE p.Name LIKE '%PAPEL HIGIENICO%' OR p.InternalCode IN ('TA006', 'TA013', 'TO006', 'TO013');

-- Check InventoryMovements for TA006 or Papel Higienico
SELECT 
    im.MovementType,
    im.Notes,
    imd.Quantity,
    p.InternalCode,
    p.Name
FROM InventoryMovementDetails imd
JOIN InventoryMovements im ON imd.InventoryMovementId = im.Id
JOIN Products p ON imd.ProductId = p.Id
WHERE p.Name LIKE '%PAPEL HIGIENICO%' OR p.InternalCode IN ('TA006', 'TA013', 'TO006', 'TO013');

-- Check PurchaseReceiptDetails for TA006 or Papel Higienico
SELECT 
    pr.ReceiptNumber,
    prd.Quantity,
    p.InternalCode,
    p.Name
FROM PurchaseReceiptDetails prd
JOIN PurchaseReceipts pr ON prd.PurchaseReceiptId = pr.Id
JOIN Products p ON prd.ProductId = p.Id
WHERE p.Name LIKE '%PAPEL HIGIENICO%' OR p.InternalCode IN ('TA006', 'TA013', 'TO006', 'TO013');
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ 1. INVENTORY ROWS FOR PAPEL HIGIENICO ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Stock: "$reader["PhysicalStock"] "| Deleted: "$reader["IsDeleted"] "| Name: "$reader["Name"]
    }
    
    $reader.NextResult()
    Write-Host "`n================ 2. MOVEMENTS FOR PAPEL HIGIENICO ================"
    while ($reader.Read()) {
        Write-Host "Type: "$reader["MovementType"] "| Qty: "$reader["Quantity"] "| Code: "$reader["InternalCode"] "| Notes: "$reader["Notes"]
    }

    $reader.NextResult()
    Write-Host "`n================ 3. PURCHASES FOR PAPEL HIGIENICO ================"
    while ($reader.Read()) {
        Write-Host "Receipt: "$reader["ReceiptNumber"] "| Qty: "$reader["Quantity"] "| Code: "$reader["InternalCode"]
    }

    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

