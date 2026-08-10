$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

# Map of InternalCode -> Target Physical Units
$targetStockMap = @{
    "GA041" = 16.0
    "CA010" = 11.0
    "GA005" = 43.0
    "GA006" = 19.0
    "GA011" = 12.0
    "GA010" = 13.0
    "GA008" = 17.0
    "GA024" = 20.0
    "GA025" = 22.0
    "GA016" = 29.0
    "GA017" = 41.0
    "GA018" = 1.0
    "GA002" = 59.0
    "GA001" = 3.0
    "GA013" = 24.0
    "GA014" = 25.0
    "GA015" = 5.0
    "GA022" = 4.0
    "GA021" = 8.0
    "GA020" = 6.0
    "CA011" = 41.0
    "MA006" = 14.0
    "MA007" = 10.0
    "MA003" = 10.0
    "MA002" = 7.0
    "CA024" = 4.0
    "CA022" = 3.0
    "CA005" = 16.0
    "CA021" = 12.0
    "CA039" = 7.0
    "CA001" = 7.0
    "CA015" = 6.0
    "CA014" = 7.0
    "GA012" = 3.0
    "MA001" = 1.0
    "CA038" = 4.0
    "CA040" = 8.0
    "CA035" = 18.0
    "CA016" = 22.0
    "CA034" = 13.0
    "CA030" = 2.0
    "CA006" = 11.0
    "GA023" = 21.0
    "CA049" = 9.0
    "CA048" = 17.0
    "CA004" = 17.0
    "CA047" = 14.0
    "CA036" = 1.0
    "CA033" = 22.0
    "CA028" = 85.0   # Mini bolo niña
    "CA029" = 90.0   # Mini bolo niño (total 175)
    "CA002" = 13.0
    "CA012" = 10.0
    "CA013" = 8.0
    "CA043" = 36.0
    "CA017" = 2.0
    "TA010" = 2.0
    "CA027" = 16.0
    "CA025" = 18.0
    "CA026" = 15.0
    "TA005" = 5.0
    "TA004" = 22.0
    "TA001" = 132.0
    "TA002" = 9.0
    "TA003" = 4.0
    "TA006" = 560.0
}

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    # 1. Get active warehouse
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = @"
SELECT TOP 1 bw.Id AS BranchWarehouseId, bw.BranchId
FROM BranchWarehouses bw
JOIN Warehouses w ON bw.WarehouseId = w.Id
WHERE bw.IsActive = 1 AND w.Name LIKE '%Exhibici%'
"@
    $reader = $cmd.ExecuteReader()
    if (-not $reader.Read()) {
        $reader.Close()
        $cmd.CommandText = "SELECT TOP 1 Id AS BranchWarehouseId, BranchId FROM BranchWarehouses WHERE IsActive = 1"
        $reader = $cmd.ExecuteReader()
        $reader.Read() | Out-Null
    }
    
    $branchWarehouseId = $reader["BranchWarehouseId"]
    $branchId = $reader["BranchId"]
    $reader.Close()

    Write-Host "Target BranchWarehouseId: $branchWarehouseId | BranchId: $branchId"

    $adjustedCount = 0
    $auditLog = @()

    foreach ($code in $targetStockMap.Keys) {
        $targetStock = [decimal]$targetStockMap[$code]

        # Get Product and Presentation info
        $cmd.CommandText = @"
SELECT p.Id AS ProductId, p.Name AS ProductName, p.DefaultUnitOfMeasureId,
       pr.Id AS PresentationId, ISNULL(pr.ConversionFactor, 1.0) AS ConversionFactor
FROM Products p
LEFT JOIN ProductPresentations pr ON p.Id = pr.ProductId AND pr.IsBaseUnit = 1 AND pr.IsDeleted = 0
WHERE UPPER(LTRIM(RTRIM(p.InternalCode))) = '$code' AND p.IsDeleted = 0
"@
        $reader = $cmd.ExecuteReader()
        if (-not $reader.Read()) {
            Write-Host "WARNING: Product $code not found in DB!"
            $reader.Close()
            continue
        }

        $productId = $reader["ProductId"]
        $productName = $reader["ProductName"]
        $uomId = $reader["DefaultUnitOfMeasureId"]
        $presentationId = $reader["PresentationId"]
        $conversionFactor = [decimal]$reader["ConversionFactor"]
        if ($conversionFactor -le 0) { $conversionFactor = 1.0 }
        $reader.Close()

        # Get current Inventory (including soft-deleted)
        $cmd.CommandText = "SELECT Id, PhysicalStock, IsDeleted FROM Inventory WHERE BranchWarehouseId = '$branchWarehouseId' AND ProductId = '$productId'"
        $reader = $cmd.ExecuteReader()
        $currentStock = 0.0
        $inventoryId = $null
        if ($reader.Read()) {
            $inventoryId = $reader["Id"]
            $currentStock = [decimal]$reader["PhysicalStock"]
        }
        $reader.Close()

        $diff = $targetStock - $currentStock
        if ([Math]::Abs($diff) -le 0.0001) {
            Write-Host "[$code] $productName - Stock matches target ($targetStock un). Skipping."
            continue
        }

        # Generate Movement Number
        $movNumCmd = $connection.CreateCommand()
        $movNumCmd.CommandText = "SELECT ISNULL(MAX(CAST(RIGHT(MovementNumber, 5) AS INT)), 0) + 1 FROM InventoryMovements WHERE MovementNumber LIKE 'MOV-%'"
        $seq = $movNumCmd.ExecuteScalar()
        $movNumber = "MOV-" + (Get-Date -Format "yyyyMMdd") + "-" + ($seq.ToString("D5"))

        $movId = [Guid]::NewGuid()
        $movDetailId = [Guid]::NewGuid()
        $mType = if ($diff -gt 0) { 3 } else { 4 } # 3 = PositiveAdjustment, 4 = NegativeAdjustment
        $qtyAbs = [Math]::Abs($diff)
        $qtyPres = $qtyAbs / $conversionFactor

        $notes = "Ajuste físico auditado: Stock anterior = $currentStock, Stock físico real = $targetStock (Dif: $diff un). Motivo: Auditoría de Inventario Físico de Agosto 2026."

        # Insert InventoryMovement
        $insertMov = $connection.CreateCommand()
        $insertMov.CommandText = @"
INSERT INTO InventoryMovements (Id, BranchId, MovementNumber, MovementType, FromBranchWarehouseId, ToBranchWarehouseId, ReferenceDocument, Notes, MovementDate, CreatedBy, CreatedOnUtc, IsDeleted)
VALUES ('$movId', '$branchId', '$movNumber', $mType, $(if ($diff -lt 0) { "'$branchWarehouseId'" } else { "NULL" }), $(if ($diff -gt 0) { "'$branchWarehouseId'" } else { "NULL" }), 'AUDIT-20260807', '$notes', GETUTCDATE(), 'AuditSystem', GETUTCDATE(), 0);
"@
        $insertMov.ExecuteNonQuery() | Out-Null

        # Insert InventoryMovementDetail
        $insertDetail = $connection.CreateCommand()
        $insertDetail.CommandText = @"
INSERT INTO InventoryMovementDetails (Id, InventoryMovementId, BranchId, ProductId, Quantity, UnitOfMeasureId, ProductPresentationId, ConversionFactor, QuantityInBaseUnit, CreatedBy, CreatedOnUtc, IsDeleted)
VALUES ('$movDetailId', '$movId', '$branchId', '$productId', $qtyPres, '$uomId', $(if ($presentationId -eq [DBNull]::Value -or $null -eq $presentationId) { "NULL" } else { "'$presentationId'" }), $conversionFactor, $qtyAbs, 'AuditSystem', GETUTCDATE(), 0);
"@
        $insertDetail.ExecuteNonQuery() | Out-Null

        # Update or Insert Inventory
        if ($null -ne $inventoryId) {
            $updateInv = $connection.CreateCommand()
            $updateInv.CommandText = "UPDATE Inventory SET PhysicalStock = $targetStock, IsDeleted = 0, LastModifiedBy = 'AuditSystem', LastModifiedOnUtc = GETUTCDATE() WHERE Id = '$inventoryId'"
            $updateInv.ExecuteNonQuery() | Out-Null
        } else {
            $newInvId = [Guid]::NewGuid()
            $insertInv = $connection.CreateCommand()
            $insertInv.CommandText = "INSERT INTO Inventory (Id, BranchWarehouseId, ProductId, PhysicalStock, ReservedStock, CommittedStock, CreatedBy, CreatedOnUtc, IsDeleted) VALUES ('$newInvId', '$branchWarehouseId', '$productId', $targetStock, 0, 0, 'AuditSystem', GETUTCDATE(), 0)"
            $insertInv.ExecuteNonQuery() | Out-Null
        }

        $adjustedCount++
        $sign = if ($diff -gt 0) { "+" } else { "" }
        Write-Host "[$code] $productName | Anterior: $currentStock | Nuevo Físico: $targetStock | Ajuste: $sign$diff un | Movimiento: $movNumber"
        $auditLog += [PSCustomObject]@{
            Code = $code
            Product = $productName
            PreviousStock = $currentStock
            TargetStock = $targetStock
            Adjustment = "$sign$diff"
            MovementNumber = $movNumber
        }
    }

    $connection.Close()
    Write-Host "`n=== AUDITED CORRECTION COMPLETED: $adjustedCount PRODUCTS ADJUSTED WITH FULL TRACEABILITY ==="
}
catch {
    Write-Host "Error executing correction:" $_.Exception.Message
}
