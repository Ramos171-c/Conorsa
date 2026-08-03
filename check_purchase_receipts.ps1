$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb_LaUnion;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    pr.[ReceiptNumber],
    pr.[ReceiptDate],
    p.[InternalCode],
    p.[Name] AS ProductName,
    prd.[Quantity],
    u.[Code] AS Uom
FROM [PurchaseReceiptDetails] prd
JOIN [PurchaseReceipts] pr ON prd.[PurchaseReceiptId] = pr.[Id]
JOIN [Products] p ON prd.[ProductId] = p.[Id]
LEFT JOIN [UnitsOfMeasure] u ON prd.[UnitOfMeasureId] = u.[Id]
ORDER BY pr.[ReceiptDate] DESC;
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ ALL PURCHASE RECEIPTS ================ "
    while ($reader.Read()) {
        Write-Host "Receipt: "$reader["ReceiptNumber"] "| Date: "$reader["ReceiptDate"] "| Code: "$reader["InternalCode"] "| Name: "$reader["ProductName"] "| Qty: "$reader["Quantity"] " " $reader["Uom"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}

