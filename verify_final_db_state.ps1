$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

# 1. Verify 26 Size SKUs
$cmd = $connection.CreateCommand()
$cmd.CommandText = @"
SELECT InternalCode, Name, CurrentCost
FROM Products
WHERE InternalCode LIKE 'TA007-%' OR InternalCode LIKE 'TA008-%' OR InternalCode LIKE 'TA011-%' OR InternalCode LIKE 'TA012-%'
ORDER BY InternalCode;
"@
$reader = $cmd.ExecuteReader()
$skus = @()
while ($reader.Read()) {
    $skus += [PSCustomObject]@{
        Code = $reader["InternalCode"]
        Name = $reader["Name"]
        Cost = $reader["CurrentCost"]
    }
}
$reader.Close()

Write-Host "Total Size-Specific Diaper SKUs in Database: $($skus.Count)"
foreach ($item in $skus) {
    Write-Host "SKU: $($item.Code) | Name: $($item.Name) | Cost: C$ $($item.Cost)"
}

# 2. Verify 17 Zeroed Products
$cmd.CommandText = @"
SELECT p.InternalCode, p.Name, i.PhysicalStock
FROM Inventory i
JOIN Products p ON i.ProductId = p.Id
WHERE p.InternalCode IN ('CA007','CA008','CA009','CA051','GA003','GA004','GA007','GA009','GA019','GA031','MA008','TA007','TA008','TA009','TA011','TA012','TA013')
ORDER BY p.InternalCode;
"@
$reader = $cmd.ExecuteReader()
$zeroed = @()
while ($reader.Read()) {
    $zeroed += [PSCustomObject]@{
        Code = $reader["InternalCode"]
        Name = $reader["Name"]
        Stock = $reader["PhysicalStock"]
    }
}
$reader.Close()

Write-Host "`nTotal Zeroed Products Checked: $($zeroed.Count)"
foreach ($item in $zeroed) {
    Write-Host "Code: $($item.Code) | Name: $($item.Name) | PhysicalStock: $($item.Stock)"
}

$connection.Close()
