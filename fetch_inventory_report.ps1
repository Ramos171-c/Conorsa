$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.[Id] AS ProductId,
    p.[InternalCode],
    p.[Name] AS ProductName,
    ISNULL(SUM(i.[PhysicalStock]), 0) AS CurrentPhysicalStock,
    (
        SELECT STRING_AGG(CONCAT(pr.[Name], ' (Factor: ', pr.[ConversionFactor], ', Base: ', pr.[IsBaseUnit], ')'), ' | ')
        FROM [ProductPresentations] pr
        WHERE pr.[ProductId] = p.[Id] AND pr.[IsDeleted] = 0
    ) AS Presentations
FROM [Products] p
LEFT JOIN [Inventory] i ON p.[Id] = i.[ProductId] AND i.[IsDeleted] = 0
WHERE p.[IsDeleted] = 0
GROUP BY p.[Id], p.[InternalCode], p.[Name]
ORDER BY p.[InternalCode];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    $items = @()
    while ($reader.Read()) {
        $items += [PSCustomObject]@{
            Id = $reader["ProductId"]
            InternalCode = $reader["InternalCode"]
            ProductName = $reader["ProductName"]
            CurrentPhysicalStock = $reader["CurrentPhysicalStock"]
            Presentations = $reader["Presentations"]
        }
    }
    $connection.Close()
    
    $items | ConvertTo-Json -Depth 5 | Out-File -FilePath "C:\Users\Isaac\.gemini\antigravity\scratch\frederman\db_products_inventory.json" -Encoding utf8
    Write-Host "Exported $($items.Count) products to db_products_inventory.json"
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
