$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$sqlScript = @"
SELECT 
    p.[InternalCode],
    p.[Name] AS ProductName,
    p.[Description],
    pr.[Name] AS PresentationName,
    u.[Code] AS UomCode,
    u.[Name] AS UomName,
    pr.[ConversionFactor],
    pr.[IsBaseUnit],
    pr.[Cost]
FROM [Products] p
LEFT JOIN [ProductPresentations] pr ON p.[Id] = pr.[ProductId]
LEFT JOIN [UnitsOfMeasure] u ON pr.[UnitOfMeasureId] = u.[Id]
WHERE p.[InternalCode] IN ('TO012', 'TA006', 'TA007', 'MA001', 'GA018') OR p.[InternalCode] LIKE 'TO%' OR p.[InternalCode] LIKE 'TA%'
ORDER BY p.[InternalCode], pr.[ConversionFactor];
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $reader = $command.ExecuteReader()
    
    Write-Host "================ PRODUCT PRESENTATIONS IN DATABASE ================"
    while ($reader.Read()) {
        Write-Host "Code: "$reader["InternalCode"] "| Name: "$reader["ProductName"] "| Pres: "$reader["PresentationName"] "| UOM: "$reader["UomCode"] "("$reader["UomName"]") | Factor: "$reader["ConversionFactor"] "| IsBase: "$reader["IsBaseUnit"]
    }
    $connection.Close()
}
catch {
    Write-Host "Error:" $_.Exception.Message
}
