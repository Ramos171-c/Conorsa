$server = "(localdb)\MSSQLLocalDB"
$connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;Integrated Security=True;TrustServerCertificate=True;"

$sqlScript = @"
BEGIN TRANSACTION;

-- 1. Resetear todas las existencias de inventario a 0.00 (sin tocar el catálogo de productos)
UPDATE [Inventory] 
SET [PhysicalStock] = 0, [ReservedStock] = 0, [CommittedStock] = 0, [LastModifiedBy] = 'SystemReset', [LastModifiedOnUtc] = GETUTCDATE();

-- 2. Eliminar detalles y pedidos del 18 de Julio hacia atrás
DELETE FROM [SalesOrderDetails] 
WHERE [SalesOrderId] IN (
    SELECT [Id] FROM [SalesOrders] WHERE [OrderDate] <= '2026-07-18 23:59:59'
);

DELETE FROM [SalesOrders] 
WHERE [OrderDate] <= '2026-07-18 23:59:59';

-- 3. Actualizar todos los pedidos restantes (del 19 de Julio en adelante) a estado 'Recibido'
UPDATE [SalesOrders] 
SET [Status] = 'Recibido', [LastModifiedBy] = 'SystemReset', [LastModifiedOnUtc] = GETUTCDATE()
WHERE [OrderDate] > '2026-07-18 23:59:59';

COMMIT TRANSACTION;

-- Conteo de verificación
SELECT 
    (SELECT COUNT(*) FROM [Inventory]) AS TotalInventarios,
    (SELECT ISNULL(SUM([PhysicalStock]), 0) FROM [Inventory]) AS TotalPhysicalStock,
    (SELECT COUNT(*) FROM [SalesOrders] WHERE [OrderDate] <= '2026-07-18 23:59:59') AS PedidosAntiguosRestantes,
    (SELECT COUNT(*) FROM [SalesOrders] WHERE [Status] = 'Recibido') AS PedidosRecibidos;
"@

Write-Host "Ejecutando depuración directa en la base de datos..."

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $command.CommandTimeout = 300
    $reader = $command.ExecuteReader()
    
    while ($reader.Read()) {
        Write-Host "================ RESULTADOS DE DEPURACIÓN EN BASE DE DATOS ================"
        Write-Host "Total Registros de Inventario :" $reader["TotalInventarios"]
        Write-Host "Suma de Existencias Físicas   :" $reader["TotalPhysicalStock"]
        Write-Host "Pedidos <= 18 Julio Restantes  :" $reader["PedidosAntiguosRestantes"]
        Write-Host "Pedidos en Estado Recibido    :" $reader["PedidosRecibidos"]
        Write-Host "=========================================================================="
    }
    $connection.Close()
    Write-Host "SUCCESS: Depuración completada exitosamente en la base de datos."
}
catch {
    Write-Host "Error durante la depuración :" $_.Exception.Message
}
