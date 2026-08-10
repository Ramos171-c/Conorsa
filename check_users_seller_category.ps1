$connStr = "Server=167.99.13.177,1433;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;Encrypt=False;TrustServerCertificate=True;Connect Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT u.Id, u.UserName, u.FirstName, u.LastName, u.SellerCategory, r.Name AS RoleName FROM Users u LEFT JOIN UserRoles ur ON u.Id = ur.UserId LEFT JOIN Roles r ON ur.RoleId = r.Id"
    $reader = $cmd.ExecuteReader()
    Write-Host "--- USUARIOS Y SUS CATEGORIAS DE VENDEDOR EN SQL SERVER ---"
    while ($reader.Read()) {
        $id = $reader.GetGuid(0)
        $user = $reader.GetString(1)
        $first = if ($reader.IsDBNull(2)) { "" } else { $reader.GetString(2) }
        $last = if ($reader.IsDBNull(3)) { "" } else { $reader.GetString(3) }
        $cat = $reader.GetInt32(4)
        $role = if ($reader.IsDBNull(5)) { "" } else { $reader.GetString(5) }
        
        $catName = if ($cat -eq 1) { "1 (Cost / Vendedor Costo)" } else { "0 (Detail / Vendedor Detalle)" }
        Write-Host "Usuario: $user | Nombre: $first $last | Rol: $role | SellerCategory: $catName"
    }
    $reader.Close()
    $conn.Close()
} catch {
    Write-Host "Error:" $_.Exception.Message
}
