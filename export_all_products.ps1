$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.Id,
    p.InternalCode,
    p.Name,
    p.Description,
    p.CurrentCost
FROM Products p
WHERE p.IsDeleted = 0
ORDER BY p.InternalCode;
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = $sqlScript
$reader = $command.ExecuteReader()

$items = @()
while ($reader.Read()) {
    $items += [PSCustomObject]@{
        Id = $reader["Id"]
        InternalCode = $reader["InternalCode"]
        Name = $reader["Name"]
        Description = $reader["Description"]
    }
}
$connection.Close()

$items | ConvertTo-Json -Depth 5 | Out-File -FilePath "C:\Users\Isaac\.gemini\antigravity\scratch\frederman\all_db_products.json" -Encoding utf8
Write-Host "Total products exported: $($items.Count)"
