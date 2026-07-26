$servers = @("167.99.13.177,1433", "(localdb)\MSSQLLocalDB")

$sqlScript = @"
BEGIN TRANSACTION;

-- 1. Eliminar todo el historial de compras, órdenes, recepciones y cuentas por pagar
DELETE FROM [PurchaseReceiptDetails];
DELETE FROM [PurchaseReceipts];

DELETE FROM [PurchaseInvoiceDetails];
DELETE FROM [PurchaseInvoices];

DELETE FROM [PurchaseOrderDetails];
DELETE FROM [PurchaseOrders];

DELETE FROM [AccountsPayablePayments];
DELETE FROM [AccountsPayables];

-- 2. Limpiar proveedores existentes
DELETE FROM [Suppliers];

-- 3. Obtener o crear una categoría de proveedor por defecto
DECLARE @CategoryId UNIQUEIDENTIFIER = (SELECT TOP 1 [Id] FROM [SupplierCategories]);
IF @CategoryId IS NULL
BEGIN
    SET @CategoryId = NEWID();
    INSERT INTO [SupplierCategories] ([Id], [Name], [Description], [IsDeleted], [CreatedBy], [CreatedOnUtc])
    VALUES (@CategoryId, 'General', 'Categoria General de Proveedores', 0, 'System', GETUTCDATE());
END

-- 4. Crear el único proveedor oficial 'Distribuidora Jenny'
INSERT INTO [Suppliers] (
    [Id], [SupplierCode], [IdentificationNumber], [IdentificationType], [Name], [LegalName], [SupplierCategoryId], [ContactName], [Email], [Phone], [Address], [Status], [IsActive], [IsDeleted], [CreatedBy], [CreatedOnUtc]
) VALUES (
    NEWID(), 'PROV-001', 'J0310000000001', 1, 'Distribuidora Jenny', 'Distribuidora Jenny S.A.', @CategoryId, 'Gerencia de Ventas', 'contacto@distribuidorajenny.com', '88888888', 'Managua, Nicaragua', 1, 1, 0, 'System', GETUTCDATE()
);

COMMIT TRANSACTION;

SELECT 
    (SELECT COUNT(*) FROM [PurchaseReceipts]) AS HistorialComprasRestante,
    (SELECT COUNT(*) FROM [Suppliers]) AS CantidadProveedores,
    (SELECT TOP 1 [Name] FROM [Suppliers]) AS ProveedorUnico;
"@

foreach ($server in $servers) {
    Write-Host "Ejecutando limpieza de compras y proveedores en $server..."
    $connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;User Id=sa;Password=Perros..1;TrustServerCertificate=True;"
    if ($server.Contains("localdb")) {
        $connectionString = "Server=$server;Database=EnterpriseBillingSystemDb;Integrated Security=True;TrustServerCertificate=True;"
    }

    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $sqlScript
        $command.CommandTimeout = 300
        $reader = $command.ExecuteReader()
        
        while ($reader.Read()) {
            Write-Host "================ RESULTADOS DE LIMPIEZA DE COMPRAS Y PROVEEDORES ($server) ================"
            Write-Host "Historial de Compras Restantes :" $reader["HistorialComprasRestante"]
            Write-Host "Cantidad de Proveedores        :" $reader["CantidadProveedores"]
            Write-Host "Nombre de Proveedor Único      :" $reader["ProveedorUnico"]
            Write-Host "=========================================================================================="
        }
        $connection.Close()
        Write-Host "SUCCESS: Limpieza de compras y proveedor único 'Distribuidora Jenny' completada en $server."
    }
    catch {
        Write-Host "Error en $server :" $_.Exception.Message
    }
}
