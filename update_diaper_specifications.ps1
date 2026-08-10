$server = "167.99.13.177,1433"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"

$specs = @(
    # --- CALSON / TRAINING PANTS (MIDDAY BEAR CALSON TA011 & CALSON OSITO TA007) ---
    @{ Code = "TA011-M";    Spec = "(6-11KG / 60 PCS)" },
    @{ Code = "TA011-L";    Spec = "(9-14KG / 58 PCS)" },
    @{ Code = "TA011-XL";   Spec = "(12-17KG / 56 PCS)" },
    @{ Code = "TA011-XXL";  Spec = "(>=15KG / 54 PCS)" },
    @{ Code = "TA011-XXXL"; Spec = "(>=17KG / 52 PCS)" },
    @{ Code = "TA011-4XL";  Spec = "(>=20KG / 46 PCS)" },

    @{ Code = "TA007-S";    Spec = "(72 PCS)" },
    @{ Code = "TA007-M";    Spec = "(6-11KG / 60 PCS)" },
    @{ Code = "TA007-L";    Spec = "(9-14KG / 58 PCS)" },
    @{ Code = "TA007-XL";   Spec = "(12-17KG / 56 PCS)" },
    @{ Code = "TA007-XXL";  Spec = "(>=15KG / 54 PCS)" },
    @{ Code = "TA007-3XL";  Spec = "(>=17KG / 52 PCS)" },
    @{ Code = "TA007-4XL";  Spec = "(>=20KG / 46 PCS)" },

    # --- PEGA PEGA / TAPE DIAPERS (PEGA PEGA OSITO TA008) ---
    @{ Code = "TA008-S";    Spec = "(4-8KG / 72 PCS)" },
    @{ Code = "TA008-M";    Spec = "(6-11KG / 64 PCS)" },
    @{ Code = "TA008-L";    Spec = "(9-14KG / 56 PCS)" },
    @{ Code = "TA008-XL";   Spec = "(12-17KG / 48 PCS)" },
    @{ Code = "TA008-XXL";  Spec = "(>=15KG / 40 PCS)" },
    @{ Code = "TA008-XXXL"; Spec = "(>=18KG / 40 PCS)" }
)

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

$updatedCount = 0

foreach ($item in $specs) {
    $code = $item.Code
    $spec = $item.Spec

    # Fetch current product details
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = "SELECT Id, Name, Description FROM Products WHERE InternalCode = '$code' AND IsDeleted = 0"
    $reader = $cmd.ExecuteReader()

    if ($reader.Read()) {
        $prodDbId = $reader["Id"]
        $name = $reader["Name"]
        $desc = $reader["Description"]
        $reader.Close()

        $newDesc = "$desc $spec"
        $shortDesc = "$name $spec"

        $updateCmd = $connection.CreateCommand()
        $updateCmd.CommandText = @"
UPDATE Products 
SET Description = '$newDesc', 
    ShortDescription = '$shortDesc', 
    LastModifiedBy = 'SystemAudit', 
    LastModifiedOnUtc = GETUTCDATE() 
WHERE Id = '$prodDbId'
"@
        $updateCmd.ExecuteNonQuery() | Out-Null
        $updatedCount++
        Write-Host "Updated [$code]: $newDesc"
    } else {
        $reader.Close()
        Write-Host "WARNING: SKU $code not found in DB."
    }
}

$connection.Close()
Write-Host "`n=== COMPLETED: UPDATED $updatedCount DIAPER SKUS WITH EXACT PIECE & WEIGHT SPECIFICATIONS ==="
