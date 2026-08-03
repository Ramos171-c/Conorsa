$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    'PurchaseReceiptDetails' AS SourceTable,
    prd.[ProductId],
    p.[InternalCode],
    p.[Name] AS ProductName,
    prd.[Quantity],
    pr.[ReceiptDate]
FROM [PurchaseReceiptDetails] prd
JOIN [PurchaseReceipts] pr ON prd.[PurchaseReceiptId] = pr.[Id]
JOIN [Products] p ON prd.[ProductId] = p.[Id]
WHERE p.[Name] LIKE '%MIDDAY%' OR p.[InternalCode] IN ('TO011', 'TA011', 'TO012')

UNION ALL

SELECT 
    'InventoryMovements' AS SourceTable,
    im.[ProductId],
    p.[InternalCode],
    p.[Name] AS ProductName,
    im.[Quantity],
    im.[MovementDate]
FROM [InventoryMovements] im
JOIN [Products] p ON im.[ProductId] = p.[Id]
WHERE p.[Name] LIKE '%MIDDAY%' OR p.[InternalCode] IN ('TO011', 'TA011', 'TO012');
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ TRANSACTIONS FOR MIDDAY / PAÑAL ================"
    while ($reader.Read()) {
        Write-Host "Source: "$reader["SourceTable"] "| Code: "$reader["InternalCode"] "| Name: "$reader["ProductName"] "| Qty: "$reader["Quantity"] "| Date: "$reader["ReceiptDate"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

