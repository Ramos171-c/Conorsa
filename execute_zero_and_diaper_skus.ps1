$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$countedCodes = @(
    "GA041", "CA010", "GA005", "GA006", "GA011", "GA010", "GA008", "GA024", "GA025", "GA016",
    "GA017", "GA018", "GA002", "GA001", "GA013", "GA014", "GA015", "GA022", "GA021", "GA020",
    "CA011", "MA006", "MA007", "MA003", "MA002", "CA024", "CA022", "CA005", "CA021", "CA039",
    "CA001", "CA015", "CA014", "GA012", "MA001", "CA038", "CA040", "CA035", "CA016", "CA034",
    "CA030", "CA006", "GA023", "CA049", "CA048", "CA004", "CA047", "CA036", "CA033", "CA028",
    "CA029", "CA002", "CA012", "CA013", "CA043", "CA017", "TA010", "CA027", "CA025", "CA026",
    "TA005", "TA004", "TA001", "TA002", "TA003", "TA006"
)

$diaperFamilies = @(
    @{
        ParentCode = "TA007"
        FamilyName = "BOLSON DE PAÑALES CALSON OSITO"
        Packaging = "(U/E: 1/4)"
        Sizes = @("S", "M", "L", "XL", "XXL", "3XL", "4XL")
    },
    @{
        ParentCode = "TA008"
        FamilyName = "BOLSON DE PAÑALES PEGA PEGA OSITO"
        Packaging = "(U/E: 1/4)"
        Sizes = @("S", "M", "L", "XL", "XXL", "XXXL")
    },
    @{
        ParentCode = "TA011"
        FamilyName = "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON"
        Packaging = "(U/E: 1*4*50)"
        Sizes = @("M", "L", "XL", "XXL", "XXXL", "4XL", "5XL", "6XL")
    },
    @{
        ParentCode = "TA012"
        FamilyName = "PAÑAL LUCAS SUPER SET"
        Packaging = "(U/E: 1*4*50)"
        Sizes = @("S", "M", "L", "XL", "XXL")
    }
)

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

# Get active branch & warehouse
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

# ==========================================
# PART 1: ZERO OUT RESIDUAL INVENTORY (17 PRODUCTS)
# ==========================================
Write-Host "`n=== PART 1: ZEROING OUT RESIDUAL INVENTORY FOR UNCOUNTED PRODUCTS ==="

$quotedCodes = ($countedCodes | ForEach-Object { "'$_'" }) -join ","
$cmd.CommandText = @"
SELECT 
    p.Id AS ProductId,
    p.InternalCode,
    p.Name AS ProductName,
    p.DefaultUnitOfMeasureId,
    i.Id AS InventoryId,
    i.PhysicalStock,
    (SELECT TOP 1 Id FROM ProductPresentations WHERE ProductId = p.Id AND IsBaseUnit = 1 AND IsDeleted = 0) AS PresentationId,
    (SELECT TOP 1 ISNULL(ConversionFactor, 1.0) FROM ProductPresentations WHERE ProductId = p.Id AND IsBaseUnit = 1 AND IsDeleted = 0) AS ConversionFactor
FROM Inventory i
JOIN Products p ON i.ProductId = p.Id
WHERE i.PhysicalStock > 0 
  AND UPPER(LTRIM(RTRIM(p.InternalCode))) NOT IN ($quotedCodes)
  AND p.IsDeleted = 0
ORDER BY p.InternalCode;
"@
$reader = $cmd.ExecuteReader()

$itemsToZero = @()
while ($reader.Read()) {
    $itemsToZero += [PSCustomObject]@{
        ProductId = $reader["ProductId"]
        InternalCode = $reader["InternalCode"]
        ProductName = $reader["ProductName"]
        DefaultUnitOfMeasureId = $reader["DefaultUnitOfMeasureId"]
        InventoryId = $reader["InventoryId"]
        PhysicalStock = [decimal]$reader["PhysicalStock"]
        PresentationId = $reader["PresentationId"]
        ConversionFactor = [decimal]$reader["ConversionFactor"]
    }
}
$reader.Close()

$zeroCount = 0
foreach ($item in $itemsToZero) {
    $code = $item.InternalCode
    $pName = $item.ProductName
    $stock = $item.PhysicalStock
    $targetProductId = $item.ProductId
    $invId = $item.InventoryId
    $uomId = $item.DefaultUnitOfMeasureId
    $presId = $item.PresentationId
    $factor = if ($item.ConversionFactor -gt 0) { $item.ConversionFactor } else { 1.0 }

    # Generate Movement Number
    $movNumCmd = $connection.CreateCommand()
    $movNumCmd.CommandText = "SELECT ISNULL(MAX(CAST(RIGHT(MovementNumber, 5) AS INT)), 0) + 1 FROM InventoryMovements WHERE MovementNumber LIKE 'MOV-%'"
    $seq = $movNumCmd.ExecuteScalar()
    $movNumber = "MOV-" + (Get-Date -Format "yyyyMMdd") + "-" + ($seq.ToString("D5"))

    $movId = [Guid]::NewGuid()
    $movDetailId = [Guid]::NewGuid()
    $notes = "Ajuste a cero auditado: Stock anterior = $stock, Stock posterior = 0. Motivo: Producto no inventariado en conteo físico del 07/08/2026."

    # Insert NegativeAdjustment InventoryMovement
    $cmd.CommandText = @"
INSERT INTO InventoryMovements (Id, BranchId, MovementNumber, MovementType, FromBranchWarehouseId, ToBranchWarehouseId, ReferenceDocument, Notes, MovementDate, CreatedBy, CreatedOnUtc, IsDeleted)
VALUES ('$movId', '$branchId', '$movNumber', 4, '$branchWarehouseId', NULL, 'AUDIT-20260807-ZERO', '$notes', GETUTCDATE(), 'AuditSystem', GETUTCDATE(), 0);
"@
    $cmd.ExecuteNonQuery() | Out-Null

    # Insert Detail
    $qtyPres = $stock / $factor
    $cmd.CommandText = @"
INSERT INTO InventoryMovementDetails (Id, InventoryMovementId, BranchId, ProductId, Quantity, UnitOfMeasureId, ProductPresentationId, ConversionFactor, QuantityInBaseUnit, CreatedBy, CreatedOnUtc, IsDeleted)
VALUES ('$movDetailId', '$movId', '$branchId', '$targetProductId', $qtyPres, '$uomId', $(if ($null -eq $presId -or $presId -eq [DBNull]::Value) { "NULL" } else { "'$presId'" }), $factor, $stock, 'AuditSystem', GETUTCDATE(), 0);
"@
    $cmd.ExecuteNonQuery() | Out-Null

    # Update Inventory PhysicalStock = 0
    $cmd.CommandText = "UPDATE Inventory SET PhysicalStock = 0, LastModifiedBy = 'AuditSystem', LastModifiedOnUtc = GETUTCDATE() WHERE Id = '$invId'"
    $cmd.ExecuteNonQuery() | Out-Null

    $zeroCount++
    Write-Host "[$code] $pName | Stock anterior: $stock -> 0.00 | Movimiento: $movNumber"
}

Write-Host "Part 1 Completed: $zeroCount products set to PhysicalStock = 0.00 with full Kardex traceability."

# ==========================================
# PART 2: CREATE 26 SIZE-SPECIFIC DIAPER SKUS
# ==========================================
Write-Host "`n=== PART 2: CREATING 26 SIZE-SPECIFIC DIAPER SKUS ==="

$createdSkusCount = 0

foreach ($fam in $diaperFamilies) {
    $parentCode = $fam.ParentCode
    $familyName = $fam.FamilyName
    $packaging = $fam.Packaging
    $sizes = $fam.Sizes

    # Query parent product details
    $cmd.CommandText = @"
SELECT Id, CurrentCost, DefaultUnitOfMeasureId, CategoryId, BrandId, TaxId, AutoMarkSoldOut, ProductType
FROM Products 
WHERE InternalCode = '$parentCode' AND IsDeleted = 0
"@
    $reader = $cmd.ExecuteReader()
    if (-not $reader.Read()) {
        Write-Host "WARNING: Parent product $parentCode not found!"
        $reader.Close()
        continue
    }

    $parentDbId = $reader["Id"]
    $cost = $reader["CurrentCost"]
    $uomId = $reader["DefaultUnitOfMeasureId"]
    $catId = $reader["CategoryId"]
    $brandId = $reader["BrandId"]
    $taxId = $reader["TaxId"]
    $pType = $reader["ProductType"]
    $reader.Close()

    # Query parent presentations
    $cmd.CommandText = "SELECT Name, ConversionFactor, Cost, RetailPrice, SemiWholesalePrice, WholesalePrice, IsBaseUnit, IsDefaultSalePresentation, UnitOfMeasureId, Barcode, AllowPurchase, AllowSale FROM ProductPresentations WHERE ProductId = '$parentDbId' AND IsDeleted = 0"
    $reader = $cmd.ExecuteReader()
    $parentPresentations = @()
    while ($reader.Read()) {
        $parentPresentations += [PSCustomObject]@{
            Name = $reader["Name"]
            ConversionFactor = $reader["ConversionFactor"]
            Cost = $reader["Cost"]
            RetailPrice = $reader["RetailPrice"]
            SemiWholesalePrice = $reader["SemiWholesalePrice"]
            WholesalePrice = $reader["WholesalePrice"]
            IsBaseUnit = $reader["IsBaseUnit"]
            IsDefaultSalePresentation = $reader["IsDefaultSalePresentation"]
            UnitOfMeasureId = $reader["UnitOfMeasureId"]
            Barcode = $reader["Barcode"]
            AllowPurchase = $reader["AllowPurchase"]
            AllowSale = $reader["AllowSale"]
        }
    }
    $reader.Close()

    # Query parent BranchProducts
    $cmd.CommandText = "SELECT BranchId, LocalSalePrice, MinSalePrice, MaxDiscountPercentage FROM BranchProducts WHERE ProductId = '$parentDbId'"
    $reader = $cmd.ExecuteReader()
    $parentBranchProducts = @()
    while ($reader.Read()) {
        $parentBranchProducts += [PSCustomObject]@{
            BranchId = $reader["BranchId"]
            LocalSalePrice = $reader["LocalSalePrice"]
            MinSalePrice = $reader["MinSalePrice"]
            MaxDiscountPercentage = $reader["MaxDiscountPercentage"]
        }
    }
    $reader.Close()

    foreach ($size in $sizes) {
        $newCode = "$parentCode-$size"
        $newName = "$familyName - TALLA $size"
        $newDesc = "$familyName - TALLA $size $packaging"

        # Check if SKU already exists
        $cmd.CommandText = "SELECT Id FROM Products WHERE InternalCode = '$newCode'"
        $existingId = $cmd.ExecuteScalar()
        if ($null -ne $existingId) {
            Write-Host "[$newCode] Already exists in DB. Skipping."
            continue
        }

        $newProductId = [Guid]::NewGuid()

        # Insert Product
        $cmd.CommandText = @"
INSERT INTO Products (
    Id, InternalCode, Name, Description, ProductType, ProductStatus, TrackInventory, 
    RequiresSerialNumber, RequiresBatchControl, CategoryId, BrandId, DefaultUnitOfMeasureId, 
    CurrentCost, IsActive, IsDeleted, CreatedBy, CreatedOnUtc, BranchId, TaxId, AutoMarkSoldOut, MinimumStock
) VALUES (
    '$newProductId', '$newCode', '$newName', '$newDesc', $pType, 1, 1, 
    0, 0, '$catId', '$brandId', '$uomId', 
    $cost, 1, 0, 'SystemAudit', GETUTCDATE(), '$branchId', '$taxId', 0, 0
)
"@
        $cmd.ExecuteNonQuery() | Out-Null

        # Insert Presentations
        foreach ($pres in $parentPresentations) {
            $newPresId = [Guid]::NewGuid()
            $presName = $pres.Name
            $pFactor = $pres.ConversionFactor
            $pCost = $pres.Cost
            $pRetail = $pres.RetailPrice
            $pSemi = $pres.SemiWholesalePrice
            $pWholesale = $pres.WholesalePrice
            $isBase = if ($pres.IsBaseUnit) { 1 } else { 0 }
            $isDefault = if ($pres.IsDefaultSalePresentation) { 1 } else { 0 }
            $presUomId = $pres.UnitOfMeasureId

            $cmd.CommandText = @"
INSERT INTO ProductPresentations (
    Id, ProductId, UnitOfMeasureId, Name, ConversionFactor, Cost, RetailPrice, 
    SemiWholesalePrice, WholesalePrice, IsBaseUnit, IsDefaultSalePresentation, 
    IsActive, IsDeleted, AllowPurchase, AllowSale, CreatedBy, CreatedOnUtc
) VALUES (
    '$newPresId', '$newProductId', '$presUomId', '$presName', $pFactor, $pCost, $pRetail, 
    $pSemi, $pWholesale, $isBase, $isDefault, 
    1, 0, 1, 1, 'SystemAudit', GETUTCDATE()
)
"@
            $cmd.ExecuteNonQuery() | Out-Null
        }

        # Insert BranchProducts
        foreach ($bp in $parentBranchProducts) {
            $bpBranchId = $bp.BranchId
            $localPrice = if ($bp.LocalSalePrice -ne $null -and $bp.LocalSalePrice -ne [DBNull]::Value) { $bp.LocalSalePrice } else { 0 }
            $minPrice = if ($bp.MinSalePrice -ne $null -and $bp.MinSalePrice -ne [DBNull]::Value) { $bp.MinSalePrice } else { 0 }
            $maxDisc = if ($bp.MaxDiscountPercentage -ne $null -and $bp.MaxDiscountPercentage -ne [DBNull]::Value) { $bp.MaxDiscountPercentage } else { 0 }

            $cmd.CommandText = @"
INSERT INTO BranchProducts (
    BranchId, ProductId, LocalSalePrice, MinSalePrice, MaxDiscountPercentage, IsActive
) VALUES (
    '$bpBranchId', '$newProductId', $localPrice, $minPrice, $maxDisc, 1
)
"@
            $cmd.ExecuteNonQuery() | Out-Null
        }

        # Insert initial Inventory record = 0
        $newInvId = [Guid]::NewGuid()
        $cmd.CommandText = @"
INSERT INTO Inventory (
    Id, BranchWarehouseId, ProductId, PhysicalStock, ReservedStock, CommittedStock, CreatedBy, CreatedOnUtc, IsDeleted
) VALUES (
    '$newInvId', '$branchWarehouseId', '$newProductId', 0, 0, 0, 'SystemAudit', GETUTCDATE(), 0
)
"@
        $cmd.ExecuteNonQuery() | Out-Null

        $createdSkusCount++
        Write-Host "Created SKU: [$newCode] - $newName"
    }
}

$connection.Close()
Write-Host "`n=== COMPLETED: $zeroCount PRODUCTS ZEROED OUT & $createdSkusCount SIZE SKUS CREATED IN DATABASE ==="
