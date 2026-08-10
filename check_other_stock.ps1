
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

$quotedCodes = ($countedCodes | ForEach-Object { "'$_'" }) -join ","

$sqlScript = @"
SELECT 
    p.InternalCode,
    p.Name AS ProductName,
    i.PhysicalStock,
    i.Id AS InventoryId
FROM Inventory i
JOIN Products p ON i.ProductId = p.Id
WHERE i.PhysicalStock > 0 
  AND UPPER(LTRIM(RTRIM(p.InternalCode))) NOT IN ($quotedCodes)
  AND p.IsDeleted = 0
ORDER BY p.InternalCode;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sqlScript
$reader = $command.ExecuteReader()

$otherItems = @()
while ($reader.Read()) {
    $otherItems += [PSCustomObject]@{
        InternalCode = $reader["InternalCode"]
        ProductName = $reader["ProductName"]
        PhysicalStock = $reader["PhysicalStock"]
        InventoryId = $reader["InventoryId"]
    }
}
$connection.Close()

Write-Host "Total items with stock > 0 not in physical count list: $($otherItems.Count)"
$otherItems | ForEach-Object {
    Write-Host "Code:" $_.InternalCode "| Name:" $_.ProductName "| Stock to set to 0:" $_.PhysicalStock
}
