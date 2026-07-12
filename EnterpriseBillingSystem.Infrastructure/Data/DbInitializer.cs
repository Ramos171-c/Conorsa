using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Infrastructure.Data;

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public DbInitializer(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitializeAsync()
    {
        // 1. Aplicar migraciones pendientes si las hubiera
        if ((await _context.Database.GetPendingMigrationsAsync()).Any())
        {
            await _context.Database.MigrateAsync();
        }

        // 1.5. Sembrar Rutas por Defecto
        if (!await _context.Routes.AnyAsync())
        {
            var defaultRoutes = new[]
            {
                new Route { Id = Guid.NewGuid(), Code = "R01", Name = "Ruta Norte", IsActive = true },
                new Route { Id = Guid.NewGuid(), Code = "R02", Name = "Ruta Sur", IsActive = true },
                new Route { Id = Guid.NewGuid(), Code = "R03", Name = "Ruta Este", IsActive = true },
                new Route { Id = Guid.NewGuid(), Code = "R04", Name = "Ruta Oeste", IsActive = true }
            };
            await _context.Routes.AddRangeAsync(defaultRoutes);
            await _context.SaveChangesAsync();
        }

        // 2. Sembrar Sucursal (Branch)
        var casaMatriz = await _context.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Code == "CM01");
        if (casaMatriz == null)
        {
            casaMatriz = new Branch
            {
                Id = Guid.NewGuid(),
                Code = "CM01",
                Name = "CASA MATRIZ",
                Address = "Avenida Central #100, Capital",
                Phone = "2222-0000",
                IsActive = true,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            await _context.Branches.AddAsync(casaMatriz);
            await _context.SaveChangesAsync();
        }

        // 3. Sembrar Permisos (Permissions)
        var permissionCodes = new[]
        {
            "users.view", "users.create", "users.edit", "users.delete",
            "roles.view", "roles.create", "roles.edit", "roles.delete",
            "branches.view", "branches.create", "branches.edit", "branches.delete",
            "products.view", "products.create", "products.edit", "products.delete",
            "categories.view", "categories.create", "categories.edit", "categories.delete",
            "brands.view", "brands.create", "brands.edit", "brands.delete",
            "taxes.view", "taxes.create", "taxes.edit", "taxes.delete",
            "units.view", "units.create", "units.edit", "units.delete",
            "inventory.view", "inventory.adjust",
            "customers.view", "customers.create", "customers.edit", "customers.delete",
            "sales.view", "sales.create", "sales.edit", "sales.post", "sales.cancel",
            "suppliers.view", "suppliers.create", "suppliers.edit",
            "purchases.view", "purchases.create", "purchases.approve", "purchases.receive",
            "cash.view", "cash.open", "cash.close", "cash.movement", "cash.manage",
            "ar.view", "ar.payment", "ar.manage",
            "ap.view", "ap.payment", "ap.manage",
            "accounting.view", "accounting.create", "accounting.edit", "accounting.post", "accounting.close-period",
            "bank.view", "bank.create", "bank.edit", "bank.deposit", "bank.withdraw", "bank.transfer", "bank.reconcile",
            "assets.view", "assets.create", "assets.edit", "assets.dispose", "assets.depreciate",
            "reports.view",
            "admin.view"
        };

        var allPermissions = new List<Permission>();
        var existingPermissions = await _context.Permissions.IgnoreQueryFilters().ToListAsync();

        foreach (var code in permissionCodes)
        {
            var permission = existingPermissions.FirstOrDefault(p => p.Code == code);
            if (permission == null)
            {
                permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = $"Permiso para {code.Replace('.', ' ')}",
                    Description = $"Permite realizar operaciones de {code.Split('.')[1]} sobre {code.Split('.')[0]}.",
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _context.Permissions.AddAsync(permission);
                allPermissions.Add(permission);
            }
            else
            {
                allPermissions.Add(permission);
            }
        }
        await _context.SaveChangesAsync();

        // 4. Sembrar Roles
        var rolesList = new[]
        {
            new { Name = "SUPER_ADMIN", Desc = "Super Administrador con control total del sistema." },
            new { Name = "ADMINISTRADOR", Desc = "Administrador de sucursal o procesos corporativos." },
            new { Name = "SUPERVISOR", Desc = "Supervisor de ventas e inventario." },
            new { Name = "CONTADOR", Desc = "Contador General de la empresa." },
            new { Name = "TESORERO", Desc = "Tesorero encargado de bancos y caja de la empresa." },
            new { Name = "VENDEDOR", Desc = "Vendedor con acceso a facturación y consulta de productos." },
            new { Name = "CAJERO", Desc = "Cajero con acceso a cobros y facturación." }
        };

        foreach (var r in rolesList)
        {
            var roleExist = await _roleManager.RoleExistsAsync(r.Name);
            if (!roleExist)
            {
                var role = new ApplicationRole(r.Name)
                {
                    Description = r.Desc,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _roleManager.CreateAsync(role);
            }
        }

        // 5. Asignar Permisos a los Roles
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM [RolePermissions]");

        // Obtener roles reales creados
        var dbRoles = await _context.Roles.IgnoreQueryFilters().ToListAsync();

        foreach (var role in dbRoles)
        {
            IEnumerable<Permission> rolePerms = role.Name switch
            {
                "SUPER_ADMIN" => allPermissions, // Acceso total
                "ADMINISTRADOR" => allPermissions.Where(p => !p.Code.EndsWith(".delete")), // Todo excepto borrar
                "SUPERVISOR" => allPermissions.Where(p => p.Code.StartsWith("products.") || p.Code.StartsWith("inventory.") || p.Code.StartsWith("customers.") || p.Code.StartsWith("sales.") || p.Code.StartsWith("suppliers.") || p.Code.StartsWith("cash.") || p.Code.StartsWith("bank.") || p.Code.StartsWith("ar.") || p.Code.StartsWith("ap.") || p.Code.StartsWith("accounting.") || p.Code.StartsWith("assets.") || p.Code == "purchases.view" || p.Code == "reports.view"),
                "CONTADOR" => allPermissions.Where(p => p.Code.StartsWith("accounting.") || p.Code.StartsWith("bank.") || p.Code.StartsWith("assets.") || p.Code == "reports.view"),
                "TESORERO" => allPermissions.Where(p => p.Code.StartsWith("bank.") || p.Code.StartsWith("cash.") || p.Code == "reports.view"),
                "VENDEDOR" => allPermissions.Where(p => p.Code == "products.view" || p.Code == "products.create" || p.Code == "products.edit" || p.Code == "categories.view" || p.Code == "taxes.view" || p.Code == "units.view" || p.Code == "customers.view" || p.Code == "customers.create" || p.Code == "customers.edit" || p.Code == "cash.view" || (p.Code.StartsWith("sales.") && p.Code != "sales.cancel") || p.Code == "ar.view" || p.Code == "users.view" || p.Code == "users.create" || p.Code == "users.edit" || p.Code == "suppliers.view" || p.Code == "purchases.view" || p.Code == "purchases.receive" || p.Code == "inventory.view" || p.Code == "admin.view"),
                "CAJERO" => allPermissions.Where(p => p.Code == "categories.view" || p.Code == "taxes.view" || p.Code == "units.view" || p.Code == "customers.view" || p.Code == "customers.create" || p.Code == "customers.edit" || p.Code.StartsWith("sales.") || (p.Code.StartsWith("cash.") && p.Code != "cash.manage") || p.Code == "ar.view" || p.Code == "ar.payment" || p.Code == "ap.view" || p.Code == "ap.payment" || p.Code == "users.view" || p.Code == "suppliers.view" || p.Code == "purchases.view" || p.Code == "inventory.view"),
                _ => Array.Empty<Permission>()
            };

            foreach (var perm in rolePerms)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO [RolePermissions] (RoleId, PermissionId) VALUES ({role.Id}, {perm.Id})");
            }
        }

        // 6. Sembrar Usuario Administrador Inicial
        var adminUser = await _userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@localhost",
                NormalizedEmail = "ADMIN@LOCALHOST",
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                DefaultBranchId = casaMatriz.Id,
                IsActive = true,
                ForcePasswordChange = false,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(adminUser, "Admin@2026#Initial");
            if (createResult.Succeeded)
            {
                // Asignar Rol
                await _userManager.AddToRoleAsync(adminUser, "SUPER_ADMIN");

                // Asignar Sucursal en UserBranches
                var userBranch = new UserBranch
                {
                    UserId = adminUser.Id,
                    BranchId = casaMatriz.Id,
                    IsDefault = true
                };
                await _context.UserBranches.AddAsync(userBranch);
                await _context.SaveChangesAsync();
            }
        }

        // 7. Sembrar/Actualizar Datos del Catálogo Real de Productos (Sincronización de Precios)
        await ResetAndSeedNewCatalogAsync(casaMatriz.Id);

        if (!await _context.Warehouses.AnyAsync())
        {
            var generalWh = new Warehouse
            {
                Id = Guid.NewGuid(),
                Code = "BG01",
                Name = "Bodega General",
                Description = "Bodega de almacenamiento principal de mercancías",
                IsActive = true,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            await _context.Warehouses.AddAsync(generalWh);
            await _context.SaveChangesAsync();

            // Asociar a la Casa Matriz
            if (casaMatriz != null)
            {
                var bgCM = new BranchWarehouse
                {
                    Id = Guid.NewGuid(),
                    BranchId = casaMatriz.Id,
                    WarehouseId = generalWh.Id,
                    IsDefault = true,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                };

                await _context.BranchWarehouses.AddAsync(bgCM);
                await _context.SaveChangesAsync();

                // Existencias iniciales se manejan dentro de ResetAndSeedNewCatalogAsync
                await _context.SaveChangesAsync();
            }
        }

        // 9. Sembrar Categorías y Clientes de Ejemplo
        if (!await _context.CustomerCategories.AnyAsync())
        {
            var profileRetail = await _context.CustomerPricingProfiles.FirstOrDefaultAsync(p => p.Type == CustomerPricingType.Retail);
            if (profileRetail == null)
            {
                profileRetail = new CustomerPricingProfile
                {
                    Id = Guid.NewGuid(),
                    Name = "Detalle",
                    Type = CustomerPricingType.Retail,
                    IsActive = true
                };
                await _context.CustomerPricingProfiles.AddAsync(profileRetail);
            }

            var profileSemiWholesale = await _context.CustomerPricingProfiles.FirstOrDefaultAsync(p => p.Type == CustomerPricingType.SemiWholesale);
            if (profileSemiWholesale == null)
            {
                profileSemiWholesale = new CustomerPricingProfile
                {
                    Id = Guid.NewGuid(),
                    Name = "Semi Mayorista",
                    Type = CustomerPricingType.SemiWholesale,
                    IsActive = true
                };
                await _context.CustomerPricingProfiles.AddAsync(profileSemiWholesale);
            }

            var profileWholesale = await _context.CustomerPricingProfiles.FirstOrDefaultAsync(p => p.Type == CustomerPricingType.Wholesale);
            if (profileWholesale == null)
            {
                profileWholesale = new CustomerPricingProfile
                {
                    Id = Guid.NewGuid(),
                    Name = "Mayorista",
                    Type = CustomerPricingType.Wholesale,
                    IsActive = true
                };
                await _context.CustomerPricingProfiles.AddAsync(profileWholesale);
            }

            await _context.SaveChangesAsync();

            var catFinal = new CustomerCategory { Id = Guid.NewGuid(), Name = "Cliente Final", Description = "Clientes generales de contado", DefaultDiscountPercentage = 0.00m, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            var catMayorista = new CustomerCategory { Id = Guid.NewGuid(), Name = "Mayorista", Description = "Clientes distribuidores mayoristas", DefaultDiscountPercentage = 5.00m, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            var catVip = new CustomerCategory { Id = Guid.NewGuid(), Name = "VIP", Description = "Clientes especiales VIP", DefaultDiscountPercentage = 10.00m, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            
            await _context.CustomerCategories.AddRangeAsync(catFinal, catMayorista, catVip);
            await _context.SaveChangesAsync();

            var juan = new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = "CUS-000001",
                IdentificationNumber = "123-456789-0001A",
                IdentificationType = IdentificationType.Cedula,
                CustomerType = CustomerType.Natural,
                Name = "Juan Pérez",
                LegalName = "Pérez",
                CustomerCategoryId = catFinal.Id,
                CustomerPricingProfileId = profileRetail.Id,
                CreditLimit = 0.0000m,
                CreditDays = 0,
                CanUseCredit = false,
                IsTaxExempt = false,
                DefaultDiscountPercentage = 0.00m,
                Status = CustomerStatus.Active,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            juan.Addresses.Add(new CustomerAddress { Id = Guid.NewGuid(), AddressLine1 = "Avenida Central #100", City = "Managua", State = "Managua", ZipCode = "10000", Country = "Nicaragua", AddressType = "Principal", IsDefault = true });
            juan.Phones.Add(new CustomerPhone { Id = Guid.NewGuid(), PhoneNumber = "8888-1111", PhoneType = "Celular", IsDefault = true });
            juan.Emails.Add(new CustomerEmail { Id = Guid.NewGuid(), EmailAddress = "juan.perez@example.com", EmailType = "Personal", IsDefault = true });

            var distribuidora = new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = "CUS-000002",
                IdentificationNumber = "J0310000000001",
                IdentificationType = IdentificationType.RUC,
                CustomerType = CustomerType.LegalEntity,
                Name = "Distribuidora Industrial S.A.",
                LegalName = "Distribuidora Industrial S.A.",
                CustomerCategoryId = catMayorista.Id,
                CustomerPricingProfileId = profileWholesale.Id,
                CreditLimit = 50000.0000m,
                CreditDays = 30,
                CanUseCredit = true,
                IsTaxExempt = false,
                DefaultDiscountPercentage = 5.00m,
                Status = CustomerStatus.Active,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            distribuidora.Addresses.Add(new CustomerAddress { Id = Guid.NewGuid(), AddressLine1 = "Km 10 Carretera Norte", City = "Managua", State = "Managua", ZipCode = "11000", Country = "Nicaragua", AddressType = "Principal", IsDefault = true });
            distribuidora.Phones.Add(new CustomerPhone { Id = Guid.NewGuid(), PhoneNumber = "2244-1234", PhoneType = "Trabajo", IsDefault = true });
            distribuidora.Emails.Add(new CustomerEmail { Id = Guid.NewGuid(), EmailAddress = "ventas@distindustrial.com", EmailType = "Facturación", IsDefault = true });
            distribuidora.Contacts.Add(new CustomerContact { Id = Guid.NewGuid(), FirstName = "Carlos", LastName = "Mendoza", JobTitle = "Gerente de Compras", Phone = "8888-2222", Email = "carlos.mendoza@distindustrial.com", IsDefault = true });

            var corporacion = new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = "CUS-000003",
                IdentificationNumber = "J0310000000003",
                IdentificationType = IdentificationType.RUC,
                CustomerType = CustomerType.LegalEntity,
                Name = "Corporación de Alimentos S.A.",
                LegalName = "Corporación de Alimentos S.A.",
                CustomerCategoryId = catMayorista.Id,
                CustomerPricingProfileId = profileSemiWholesale.Id,
                CreditLimit = 25000.0000m,
                CreditDays = 15,
                CanUseCredit = true,
                IsTaxExempt = false,
                DefaultDiscountPercentage = 5.00m,
                Status = CustomerStatus.Active,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            corporacion.Addresses.Add(new CustomerAddress { Id = Guid.NewGuid(), AddressLine1 = "Plaza El Sol 2c Sur", City = "Managua", State = "Managua", ZipCode = "12000", Country = "Nicaragua", AddressType = "Principal", IsDefault = true });
            corporacion.Phones.Add(new CustomerPhone { Id = Guid.NewGuid(), PhoneNumber = "2255-6789", PhoneType = "Trabajo", IsDefault = true });
            corporacion.Emails.Add(new CustomerEmail { Id = Guid.NewGuid(), EmailAddress = "compras@corpalimentos.com", EmailType = "Facturación", IsDefault = true });
            corporacion.Contacts.Add(new CustomerContact { Id = Guid.NewGuid(), FirstName = "Ana", LastName = "Gómez", JobTitle = "Compradora Principal", Phone = "8888-3333", Email = "ana.gomez@corpalimentos.com", IsDefault = true });

            var exento = new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = "CUS-000004",
                IdentificationNumber = "J0310000000002",
                IdentificationType = IdentificationType.RUC,
                CustomerType = CustomerType.LegalEntity,
                Name = "Organización de Ayuda Social",
                LegalName = "Organización de Ayuda Social",
                CustomerCategoryId = catVip.Id,
                CustomerPricingProfileId = profileRetail.Id,
                CreditLimit = 0.0000m,
                CreditDays = 0,
                CanUseCredit = false,
                IsTaxExempt = true,
                DefaultDiscountPercentage = 10.00m,
                Status = CustomerStatus.Active,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            exento.Addresses.Add(new CustomerAddress { Id = Guid.NewGuid(), AddressLine1 = "Costado Oeste de la Catedral", City = "León", State = "León", ZipCode = "20000", Country = "Nicaragua", AddressType = "Principal", IsDefault = true });
            exento.Phones.Add(new CustomerPhone { Id = Guid.NewGuid(), PhoneNumber = "2311-4567", PhoneType = "Trabajo", IsDefault = true });
            exento.Emails.Add(new CustomerEmail { Id = Guid.NewGuid(), EmailAddress = "info@ayudasocial.org", EmailType = "Personal", IsDefault = true });
            exento.Contacts.Add(new CustomerContact { Id = Guid.NewGuid(), FirstName = "María", LastName = "López", JobTitle = "Directora", Phone = "8888-4444", Email = "maria.lopez@ayudasocial.org", IsDefault = true });

            await _context.Customers.AddRangeAsync(juan, distribuidora, corporacion, exento);
            await _context.SaveChangesAsync();
        }

        // 9. Sembrar Datos de Ejemplo de Proveedores
        if (!await _context.SupplierCategories.AnyAsync())
        {
            var catTech = new SupplierCategory
            {
                Id = Guid.NewGuid(),
                Name = "Tecnología",
                Description = "Proveedores de equipos y suministros tecnológicos",
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            var catInsumos = new SupplierCategory
            {
                Id = Guid.NewGuid(),
                Name = "Insumos Generales",
                Description = "Proveedores de insumos y materiales varios",
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            await _context.SupplierCategories.AddRangeAsync(catTech, catInsumos);
            await _context.SaveChangesAsync();

            // Proveedores de ejemplo
            var supplier1 = new Supplier
            {
                Id = Guid.NewGuid(),
                SupplierCode = "SUP-000001",
                IdentificationNumber = "J0310000001234",
                IdentificationType = IdentificationType.RUC,
                Name = "Tech Distribuidores S.A.",
                LegalName = "Tech Distribuidores Sociedad Anónima",
                SupplierCategoryId = catTech.Id,
                Phone = "2222-1111",
                Email = "ventas@techdist.com",
                Address = "Zona Industrial Norte, Managua",
                ContactName = "Carlos Rodríguez",
                Status = SupplierStatus.Active,
                IsActive = true,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            var supplier2 = new Supplier
            {
                Id = Guid.NewGuid(),
                SupplierCode = "SUP-000002",
                IdentificationNumber = "1234567890",
                IdentificationType = IdentificationType.Cedula,
                Name = "Insumos El Progreso",
                LegalName = null,
                SupplierCategoryId = catInsumos.Id,
                Phone = "8888-5555",
                Email = "insumos.progreso@gmail.com",
                Address = "Mercado Oriental, Managua",
                ContactName = "Pedro Martínez",
                Status = SupplierStatus.Active,
                IsActive = true,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };

            await _context.Suppliers.AddRangeAsync(supplier1, supplier2);
            await _context.SaveChangesAsync();
        }

        // 10. Sembrar Pedido de Venta y Factura demo (Draft)
        if (!await _context.SalesOrders.AnyAsync())
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUS-000002");
            var productDell = await _context.Products.FirstOrDefaultAsync(p => p.InternalCode == "PROD-DELL-01");
            var uomUnd = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Code == "UND");
            var bgCM = await _context.BranchWarehouses.FirstOrDefaultAsync();

            if (customer != null && productDell != null && uomUnd != null && bgCM != null)
            {
                // Pedido demo
                var order = new SalesOrder
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = "SO-20260616-00001",
                    CustomerId = customer.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = SalesOrderStatus.Recibido,
                    SubTotal = 1200.0000m,
                    DiscountAmount = 60.0000m,
                    TaxAmount = 182.4000m,
                    TotalAmount = 1322.4000m,
                    Notes = "Pedido de prueba sembrado automáticamente",
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                };

                order.Details.Add(new SalesOrderDetail
                {
                    Id = Guid.NewGuid(),
                    SalesOrderId = order.Id,
                    ProductId = productDell.Id,
                    UnitOfMeasureId = uomUnd.Id,
                    Quantity = 1.0000m,
                    UnitPrice = 1200.0000m,
                    DiscountPercentage = 5.00m,
                    DiscountAmount = 60.0000m,
                    TaxPercentage = 16.00m,
                    TaxAmount = 182.4000m,
                    NetAmount = 1322.4000m
                });

                await _context.SalesOrders.AddAsync(order);

                // Factura demo (Borrador)
                var invoice = new SalesInvoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = "INV-20260616-00001",
                    CustomerId = customer.Id,
                    CustomerNameSnapshot = customer.Name,
                    CustomerIdentificationSnapshot = customer.IdentificationNumber,
                    BranchWarehouseId = bgCM.Id,
                    SalesOrderId = null,
                    InvoiceDate = DateTime.UtcNow,
                    IsCreditSale = true,
                    PaymentTermsDays = 30,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    Status = SalesInvoiceStatus.Draft,
                    SubTotal = 1200.0000m,
                    DiscountAmount = 60.0000m,
                    TaxAmount = 182.4000m,
                    TotalAmount = 1322.4000m,
                    Notes = "Factura de prueba borrador sembrada automáticamente",
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                };

                invoice.Details.Add(new SalesInvoiceDetail
                {
                    Id = Guid.NewGuid(),
                    SalesInvoiceId = invoice.Id,
                    ProductId = productDell.Id,
                    UnitOfMeasureId = uomUnd.Id,
                    ProductPresentationId = productDell.Presentations.First().Id,
                    Quantity = 1.0000m,
                    UnitPrice = 1200.0000m,
                    DiscountPercentage = 5.00m,
                    DiscountAmount = 60.0000m,
                    TaxPercentage = 16.00m,
                    TaxAmount = 182.4000m,
                    NetAmount = 1322.4000m,
                    ProductCodeSnapshot = productDell.InternalCode,
                    ProductNameSnapshot = productDell.Name,
                    UnitOfMeasureSnapshot = uomUnd.Code
                });

                await _context.SalesInvoices.AddAsync(invoice);
                await _context.SaveChangesAsync();
            }
        }

        // 11. Sembrar Métodos de Pago
        if (!await _context.PaymentMethods.AnyAsync())
        {
            var methods = new List<PaymentMethod>
            {
                new PaymentMethod { Id = Guid.NewGuid(), Code = "EFEC", Name = "Efectivo", IsCash = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow },
                new PaymentMethod { Id = Guid.NewGuid(), Code = "TARJ", Name = "Tarjeta de Crédito/Débito", IsCash = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow },
                new PaymentMethod { Id = Guid.NewGuid(), Code = "TRAN", Name = "Transferencia Bancaria", IsCash = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow },
                new PaymentMethod { Id = Guid.NewGuid(), Code = "POS", Name = "POS Bancario", IsCash = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow }
            };
            await _context.PaymentMethods.AddRangeAsync(methods);
            await _context.SaveChangesAsync();
        }

        // 12. Sembrar Cajas Físicas y Sesión Demo (Open)
        if (!await _context.CashRegisters.AnyAsync())
        {
            var branch = await _context.Branches.FirstOrDefaultAsync();
            var cashAdminUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
            var cashMethod = await _context.PaymentMethods.FirstOrDefaultAsync(p => p.Code == "EFEC");

            if (branch != null && cashAdminUser != null && cashMethod != null)
            {
                // Caja Principal
                var register = new CashRegister
                {
                    Id = Guid.NewGuid(),
                    Code = "CAJA-CM-01",
                    Name = "Caja Principal Casa Matriz",
                    BranchId = branch.Id,
                    IsDefault = true,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                };
                await _context.CashRegisters.AddAsync(register);
                await _context.SaveChangesAsync();

                // Sesión demo de apertura
                var session = new CashSession
                {
                    Id = Guid.NewGuid(),
                    SessionNumber = "CS-20260616-00001",
                    CashRegisterId = register.Id,
                    OpenedByUserId = cashAdminUser.Id,
                    OpeningAmount = 1000.0000m,
                    OpenedAt = DateTime.UtcNow,
                    Status = CashSessionStatus.Open,
                    Notes = "Apertura inicial sembrada automáticamente",
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                };

                session.CashMovements.Add(new CashMovement
                {
                    Id = Guid.NewGuid(),
                    CashSessionId = session.Id,
                    MovementType = CashMovementType.Opening,
                    PaymentMethodId = cashMethod.Id,
                    Amount = 1000.0000m,
                    Notes = "Saldo inicial de apertura de sesión (semilla)",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.CashSessions.AddAsync(session);
                await _context.SaveChangesAsync();
            }
        }

        // 13. Sembrar Cuenta por Cobrar Demo y Pago Parcial Demo
        if (!await _context.AccountsReceivables.AnyAsync())
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUS-000002");
            var productDell = await _context.Products.FirstOrDefaultAsync(p => p.InternalCode == "PROD-DELL-01");
            var uomUnd = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Code == "UND");
            var bgCM = await _context.BranchWarehouses.FirstOrDefaultAsync();
            var openSession = await _context.CashSessions.FirstOrDefaultAsync(s => s.Status == CashSessionStatus.Open);
            var paymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync(p => p.Code == "EFEC");

            if (customer != null && productDell != null && uomUnd != null && bgCM != null && openSession != null && paymentMethod != null)
            {
                // Crear factura crédito sembrada (Posted)
                var invoice = new SalesInvoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = "INV-CRED-001",
                    CustomerId = customer.Id,
                    CustomerNameSnapshot = customer.Name,
                    CustomerIdentificationSnapshot = customer.IdentificationNumber,
                    BranchWarehouseId = bgCM.Id,
                    SalesOrderId = null,
                    InvoiceDate = DateTime.UtcNow.AddDays(-10), // Hace 10 días
                    IsCreditSale = true,
                    PaymentTermsDays = 30,
                    DueDate = DateTime.UtcNow.AddDays(20), // Vence en 20 días
                    Status = SalesInvoiceStatus.Posted,
                    SubTotal = 4000.0000m,
                    DiscountAmount = 0.0000m,
                    TaxAmount = 640.0000m,
                    TotalAmount = 4640.0000m,
                    Notes = "Factura crédito demo sembrada",
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow.AddDays(-10)
                };

                invoice.Details.Add(new SalesInvoiceDetail
                {
                    Id = Guid.NewGuid(),
                    SalesInvoiceId = invoice.Id,
                    ProductId = productDell.Id,
                    UnitOfMeasureId = uomUnd.Id,
                    ProductPresentationId = productDell.Presentations.First().Id,
                    Quantity = 4.0000m,
                    UnitPrice = 1000.0000m,
                    DiscountPercentage = 0.00m,
                    DiscountAmount = 0.0000m,
                    TaxPercentage = 16.00m,
                    TaxAmount = 640.0000m,
                    NetAmount = 4640.0000m,
                    ProductCodeSnapshot = productDell.InternalCode,
                    ProductNameSnapshot = productDell.Name,
                    UnitOfMeasureSnapshot = uomUnd.Code
                });

                await _context.SalesInvoices.AddAsync(invoice);

                // Crear Cuenta por Cobrar
                var ar = new AccountsReceivable
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    SalesInvoiceId = invoice.Id,
                    DocumentNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    DueDate = invoice.DueDate ?? invoice.InvoiceDate.AddDays(invoice.PaymentTermsDays),
                    OriginalAmount = invoice.TotalAmount,
                    PaidAmount = 2000.0000m, // Abono parcial demo de $2000
                    CurrentBalance = invoice.TotalAmount - 2000.0000m, // 2640.00
                    Status = AccountsReceivableStatus.PartiallyPaid,
                    LastPaymentDate = DateTime.UtcNow.AddDays(-2), // Hace 2 días
                    Notes = "Cuenta por cobrar demo sembrada con abono parcial",
                    RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 },
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow.AddDays(-10)
                };

                // Crear Pago Parcial
                var payment = new AccountsReceivablePayment
                {
                    Id = Guid.NewGuid(),
                    AccountsReceivableId = ar.Id,
                    CashSessionId = openSession.Id,
                    PaymentMethodId = paymentMethod.Id,
                    PaymentDate = DateTime.UtcNow.AddDays(-2),
                    Amount = 2000.0000m,
                    ReferenceNumber = "REC-DEMO-001",
                    Notes = "Abono parcial demo a factura INV-CRED-001"
                };

                ar.Payments.Add(payment);

                await _context.AccountsReceivables.AddAsync(ar);

                // Crear Movimiento de Caja asociado
                var cashMovement = new CashMovement
                {
                    Id = Guid.NewGuid(),
                    CashSessionId = openSession.Id,
                    MovementType = CashMovementType.CustomerPayment,
                    PaymentMethodId = paymentMethod.Id,
                    ReferenceDocument = ar.DocumentNumber,
                    ReferenceId = ar.Id,
                    Amount = 2000.0000m, // Siempre positivo
                    Reason = "Abono de Cliente a Cuenta por Cobrar (Demo)",
                    Notes = $"Abono a CxC {ar.DocumentNumber}. Cliente: {customer.Name}. Ref: REC-DEMO-001",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                };

                await _context.CashMovements.AddAsync(cashMovement);

                await _context.SaveChangesAsync();
            }
        }

        // 14. Sembrar Factura de Compra, Cuenta por Pagar y Abono demo
        if (!await _context.AccountsPayables.AnyAsync())
        {
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.SupplierCode == "SUP-000001");
            var productDell = await _context.Products.FirstOrDefaultAsync(p => p.InternalCode == "PROD-DELL-01");
            var uomUnd = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Code == "UND");
            var bgCM = await _context.BranchWarehouses.FirstOrDefaultAsync();
            var openSession = await _context.CashSessions.FirstOrDefaultAsync(s => s.Status == CashSessionStatus.Open);
            var paymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync(p => p.Code == "EFEC");

            if (supplier != null && productDell != null && uomUnd != null && bgCM != null && openSession != null && paymentMethod != null)
            {
                // Crear factura de compra Posted demo
                var purchaseInvoice = new PurchaseInvoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = "FAC-PROV-9999", // Número físico del proveedor
                    InternalInvoiceNumber = "FC-00000001", // Interno
                    SupplierId = supplier.Id,
                    PurchaseReceiptId = null,
                    PurchaseOrderId = null,
                    InvoiceDate = DateTime.UtcNow.AddDays(-15), // Hace 15 días
                    PaymentTermsDays = 30,
                    DueDate = DateTime.UtcNow.AddDays(15), // Vence en 15 días
                    SubTotal = 3000.0000m,
                    DiscountAmount = 0.0000m,
                    TaxAmount = 480.0000m,
                    TotalAmount = 3480.0000m,
                    Status = PurchaseInvoiceStatus.Posted,
                    Notes = "Factura de compra Posted de ejemplo (semilla)",
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow.AddDays(-15),
                    BranchId = bgCM.BranchId
                };

                purchaseInvoice.Details.Add(new PurchaseInvoiceDetail
                {
                    Id = Guid.NewGuid(),
                    PurchaseInvoiceId = purchaseInvoice.Id,
                    ProductId = productDell.Id,
                    Quantity = 3.0000m,
                    UnitOfMeasureId = uomUnd.Id,
                    ProductPresentationId = productDell.Presentations.First().Id,
                    UnitPrice = 1000.0000m,
                    DiscountPercentage = 0.00m,
                    DiscountAmount = 0.0000m,
                    TaxPercentage = 16.00m,
                    TaxAmount = 480.0000m,
                    NetAmount = 3480.0000m
                });

                await _context.PurchaseInvoices.AddAsync(purchaseInvoice);

                // Crear Cuenta por Pagar demo
                var ap = new AccountsPayable
                {
                    Id = Guid.NewGuid(),
                    SupplierId = supplier.Id,
                    PurchaseInvoiceId = purchaseInvoice.Id,
                    DocumentNumber = purchaseInvoice.InvoiceNumber,
                    InvoiceDate = purchaseInvoice.InvoiceDate,
                    DueDate = purchaseInvoice.DueDate.Value,
                    OriginalAmount = purchaseInvoice.TotalAmount,
                    PaidAmount = 1000.0000m,
                    CurrentBalance = purchaseInvoice.TotalAmount - 1000.0000m,
                    Status = AccountsPayableStatus.PartiallyPaid,
                    LastPaymentDate = DateTime.UtcNow.AddDays(-5),
                    Notes = "Cuenta por pagar demo sembrada con abono parcial",
                    RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 },
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow.AddDays(-15),
                    BranchId = purchaseInvoice.BranchId
                };

                // Crear Pago Parcial demo
                var payment = new AccountsPayablePayment
                {
                    Id = Guid.NewGuid(),
                    AccountsPayableId = ap.Id,
                    CashSessionId = openSession.Id,
                    PaymentMethodId = paymentMethod.Id,
                    PaymentDate = DateTime.UtcNow.AddDays(-5),
                    Amount = 1000.0000m,
                    ReferenceNumber = "PAGO-PROV-001",
                    Notes = "Abono parcial demo a proveedor en efectivo"
                };

                ap.Payments.Add(payment);

                await _context.AccountsPayables.AddAsync(ap);

                // Crear Movimiento de Caja asociado (salida de caja de tipo SupplierRefund)
                var cashMovement = new CashMovement
                {
                    Id = Guid.NewGuid(),
                    CashSessionId = openSession.Id,
                    MovementType = CashMovementType.SupplierRefund,
                    PaymentMethodId = paymentMethod.Id,
                    ReferenceDocument = ap.DocumentNumber,
                    ReferenceId = ap.Id,
                    Amount = 1000.0000m,
                    Reason = "Pago a Proveedor (Demo)",
                    Notes = $"Pago a CxP {ap.DocumentNumber}. Proveedor: {supplier.Name}. Ref: PAGO-PROV-001",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                };

                await _context.CashMovements.AddAsync(cashMovement);

                await _context.SaveChangesAsync();
            }
        }

        // 15. Sembrar Plan de Cuentas base
        if (!await _context.Accounts.AnyAsync())
        {
            var accounts = new List<Account>();

            // Activos
            var acc1000 = new Account { Id = Guid.NewGuid(), Code = "1000", Name = "Activos", AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 1, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc1000);

            var acc1100 = new Account { Id = Guid.NewGuid(), Code = "1100", Name = "Caja", ParentAccountId = acc1000.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc1100);

            var acc1110 = new Account { Id = Guid.NewGuid(), Code = "1110", Name = "Caja General", ParentAccountId = acc1100.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc1110);

            var acc1200 = new Account { Id = Guid.NewGuid(), Code = "1200", Name = "Cuentas por Cobrar", ParentAccountId = acc1000.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc1200);

            var acc1300 = new Account { Id = Guid.NewGuid(), Code = "1300", Name = "Inventarios", ParentAccountId = acc1000.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc1300);

            // Pasivos
            var acc2000 = new Account { Id = Guid.NewGuid(), Code = "2000", Name = "Pasivos", AccountType = AccountType.Liability, Nature = AccountNature.Credit, Level = 1, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc2000);

            var acc2100 = new Account { Id = Guid.NewGuid(), Code = "2100", Name = "Cuentas por Pagar", ParentAccountId = acc2000.Id, AccountType = AccountType.Liability, Nature = AccountNature.Credit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc2100);

            // Patrimonio
            var acc3000 = new Account { Id = Guid.NewGuid(), Code = "3000", Name = "Patrimonio", AccountType = AccountType.Equity, Nature = AccountNature.Credit, Level = 1, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc3000);

            // Ingresos
            var acc4000 = new Account { Id = Guid.NewGuid(), Code = "4000", Name = "Ingresos", AccountType = AccountType.Revenue, Nature = AccountNature.Credit, Level = 1, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc4000);

            var acc4100 = new Account { Id = Guid.NewGuid(), Code = "4100", Name = "Ventas", ParentAccountId = acc4000.Id, AccountType = AccountType.Revenue, Nature = AccountNature.Credit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc4100);

            // Costos
            var acc5000 = new Account { Id = Guid.NewGuid(), Code = "5000", Name = "Costos", AccountType = AccountType.Expense, Nature = AccountNature.Debit, Level = 1, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc5000);

            var acc5100 = new Account { Id = Guid.NewGuid(), Code = "5100", Name = "Costo de Ventas", ParentAccountId = acc5000.Id, AccountType = AccountType.Expense, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc5100);

            // Gastos
            var acc6000 = new Account { Id = Guid.NewGuid(), Code = "6000", Name = "Gastos", AccountType = AccountType.Expense, Nature = AccountNature.Debit, Level = 1, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc6000);

            var acc6100 = new Account { Id = Guid.NewGuid(), Code = "6100", Name = "Ajustes Operativos", ParentAccountId = acc6000.Id, AccountType = AccountType.Expense, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            accounts.Add(acc6100);

            await _context.Accounts.AddRangeAsync(accounts);
            await _context.SaveChangesAsync();
        }

        // Sembrar cuentas contables requeridas para bancos si no existen
        var active1000 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1000");
        var active6000 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "6000");
        var active4000 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "4000");

        if (active1000 != null)
        {
            var acc1120 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1120");
            if (acc1120 == null)
            {
                acc1120 = new Account { Id = Guid.NewGuid(), Code = "1120", Name = "Bancos", ParentAccountId = active1000.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
                await _context.Accounts.AddAsync(acc1120);
                await _context.SaveChangesAsync();
            }

            var acc1121 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1121");
            if (acc1121 == null)
            {
                acc1121 = new Account { Id = Guid.NewGuid(), Code = "1121", Name = "Banco Nacional", ParentAccountId = acc1120.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
                await _context.Accounts.AddAsync(acc1121);
            }

            var acc1122 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1122");
            if (acc1122 == null)
            {
                acc1122 = new Account { Id = Guid.NewGuid(), Code = "1122", Name = "Banco BAC", ParentAccountId = acc1120.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
                await _context.Accounts.AddAsync(acc1122);
            }

            var acc1130 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1130");
            if (acc1130 == null)
            {
                acc1130 = new Account { Id = Guid.NewGuid(), Code = "1130", Name = "Bancos en Tránsito", ParentAccountId = acc1120.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
                await _context.Accounts.AddAsync(acc1130);
            }
        }

        if (active6000 != null)
        {
            var acc6105 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "6105");
            if (acc6105 == null)
            {
                acc6105 = new Account { Id = Guid.NewGuid(), Code = "6105", Name = "Gastos Bancarios", ParentAccountId = active6000.Id, AccountType = AccountType.Expense, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
                await _context.Accounts.AddAsync(acc6105);
            }
        }

        if (active4000 != null)
        {
            var acc4200 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "4200");
            if (acc4200 == null)
            {
                acc4200 = new Account { Id = Guid.NewGuid(), Code = "4200", Name = "Ingresos Financieros", ParentAccountId = active4000.Id, AccountType = AccountType.Revenue, Nature = AccountNature.Credit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
                await _context.Accounts.AddAsync(acc4200);
            }
        }
        await _context.SaveChangesAsync();

        // Sembrar Banco y Cuenta Bancaria demo
        var demoBank = await _context.Banks.FirstOrDefaultAsync(b => b.Code == "BANCO-001");
        if (demoBank == null)
        {
            demoBank = new Bank
            {
                Id = Guid.NewGuid(),
                Code = "BANCO-001",
                Name = "Banco Nacional",
                SwiftCode = "BANCNO",
                Country = "Nicaragua",
                IsActive = true,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            await _context.Banks.AddAsync(demoBank);
            await _context.SaveChangesAsync();
        }

        var demoAccount = await _context.BankAccounts.FirstOrDefaultAsync(ba => ba.AccountNumber == "CTA-001");
        if (demoAccount == null)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync();
            demoAccount = new BankAccount
            {
                Id = Guid.NewGuid(),
                BankId = demoBank.Id,
                AccountNumber = "CTA-001",
                AccountName = "Banco Nacional - Principal",
                CurrencyCode = "NIO",
                CurrentBalance = 10000.0000m,
                AccountingAccountCode = "1121",
                IsActive = true,
                BranchId = branch?.Id,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            await _context.BankAccounts.AddAsync(demoAccount);

            var openTx = new BankTransaction
            {
                Id = Guid.NewGuid(),
                BankAccountId = demoAccount.Id,
                TransactionDate = DateTime.UtcNow,
                TransactionType = BankTransactionType.Deposit,
                Amount = 10000.0000m,
                ReferenceNumber = "DEP-INI-001",
                Description = "Apertura de balance inicial (demo)",
                BranchId = branch?.Id,
                CreatedBy = "System",
                CreatedOnUtc = DateTime.UtcNow
            };
            await _context.BankTransactions.AddAsync(openTx);
            await _context.SaveChangesAsync();
        }
        // ======================================================
        // Módulo Activos Fijos — Cuentas Contables
        // ======================================================
        var faRoot1000 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1000");
        var faRoot3000 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "3000");
        var faRoot5000 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "5000");
        var faRoot6000 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "6000");

        // 1300 Activos Fijos (raíz)
        var faAcc1300 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1300");
        if (faAcc1300 == null)
        {
            faAcc1300 = new Account { Id = Guid.NewGuid(), Code = "1300", Name = "Activos Fijos", ParentAccountId = faRoot1000?.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = false, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1300);
            await _context.SaveChangesAsync();
        }

        // 1310 Vehículos
        var faAcc1310 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1310");
        if (faAcc1310 == null)
        {
            faAcc1310 = new Account { Id = Guid.NewGuid(), Code = "1310", Name = "Vehículos", ParentAccountId = faAcc1300.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1310);
        }
        var faAcc1310d = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1310.1");
        if (faAcc1310d == null)
        {
            faAcc1310d = new Account { Id = Guid.NewGuid(), Code = "1310.1", Name = "Depreciación Acumulada Vehículos", ParentAccountId = faAcc1300.Id, AccountType = AccountType.Asset, Nature = AccountNature.Credit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1310d);
        }

        // 1320 Equipos de Cómputo
        var faAcc1320 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1320");
        if (faAcc1320 == null)
        {
            faAcc1320 = new Account { Id = Guid.NewGuid(), Code = "1320", Name = "Equipos de Cómputo", ParentAccountId = faAcc1300.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1320);
        }
        var faAcc1320d = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1320.1");
        if (faAcc1320d == null)
        {
            faAcc1320d = new Account { Id = Guid.NewGuid(), Code = "1320.1", Name = "Depreciación Acumulada Equipos de Cómputo", ParentAccountId = faAcc1300.Id, AccountType = AccountType.Asset, Nature = AccountNature.Credit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1320d);
        }

        // 1330 Mobiliario
        var faAcc1330 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1330");
        if (faAcc1330 == null)
        {
            faAcc1330 = new Account { Id = Guid.NewGuid(), Code = "1330", Name = "Mobiliario y Equipo de Oficina", ParentAccountId = faAcc1300.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1330);
        }
        var faAcc1330d = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1330.1");
        if (faAcc1330d == null)
        {
            faAcc1330d = new Account { Id = Guid.NewGuid(), Code = "1330.1", Name = "Depreciación Acumulada Mobiliario", ParentAccountId = faAcc1300.Id, AccountType = AccountType.Asset, Nature = AccountNature.Credit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1330d);
        }

        // 1340 Infraestructura
        var faAcc1340 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1340");
        if (faAcc1340 == null)
        {
            faAcc1340 = new Account { Id = Guid.NewGuid(), Code = "1340", Name = "Infraestructura y Mejoras", ParentAccountId = faAcc1300.Id, AccountType = AccountType.Asset, Nature = AccountNature.Debit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1340);
        }
        var faAcc1340d = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "1340.1");
        if (faAcc1340d == null)
        {
            faAcc1340d = new Account { Id = Guid.NewGuid(), Code = "1340.1", Name = "Depreciación Acumulada Infraestructura", ParentAccountId = faAcc1300.Id, AccountType = AccountType.Asset, Nature = AccountNature.Credit, Level = 3, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc1340d);
        }

        // 5200 Gastos de Depreciación
        var faAcc5200 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "5200");
        if (faAcc5200 == null)
        {
            faAcc5200 = new Account { Id = Guid.NewGuid(), Code = "5200", Name = "Gastos de Depreciación", ParentAccountId = faRoot5000?.Id, AccountType = AccountType.Expense, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc5200);
        }

        // 3200 Superávit por Revalorización
        var faAcc3200 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "3200");
        if (faAcc3200 == null)
        {
            faAcc3200 = new Account { Id = Guid.NewGuid(), Code = "3200", Name = "Superávit por Revalorización de Activos", ParentAccountId = faRoot3000?.Id, AccountType = AccountType.Equity, Nature = AccountNature.Credit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc3200);
        }

        // 6200 Pérdida por Deterioro
        var faAcc6200 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "6200");
        if (faAcc6200 == null)
        {
            faAcc6200 = new Account { Id = Guid.NewGuid(), Code = "6200", Name = "Pérdida por Deterioro de Activos", ParentAccountId = faRoot6000?.Id, AccountType = AccountType.Expense, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc6200);
        }

        // 6210 Pérdida en Venta/Baja de Activos
        var faAcc6210 = await _context.Accounts.FirstOrDefaultAsync(a => a.Code == "6210");
        if (faAcc6210 == null)
        {
            faAcc6210 = new Account { Id = Guid.NewGuid(), Code = "6210", Name = "Pérdida en Baja de Activos Fijos", ParentAccountId = faRoot6000?.Id, AccountType = AccountType.Expense, Nature = AccountNature.Debit, Level = 2, IsPostingAccount = true, IsActive = true, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
            await _context.Accounts.AddAsync(faAcc6210);
        }

        await _context.SaveChangesAsync();

        // ======================================================
        // Módulo Activos Fijos — Categorías Demo
        // ======================================================
        if (!await _context.FixedAssetCategories.AnyAsync())
        {
            var categories = new[]
            {
                new FixedAssetCategory
                {
                    Id = Guid.NewGuid(),
                    Code = "VEH",
                    Name = "Vehículos",
                    AssetAccountCode = "1310",
                    AccumulatedDepreciationAccountCode = "1310.1",
                    DepreciationExpenseAccountCode = "5200",
                    UsefulLifeMonths = 60,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                },
                new FixedAssetCategory
                {
                    Id = Guid.NewGuid(),
                    Code = "EQP",
                    Name = "Equipos de Cómputo",
                    AssetAccountCode = "1320",
                    AccumulatedDepreciationAccountCode = "1320.1",
                    DepreciationExpenseAccountCode = "5200",
                    UsefulLifeMonths = 36,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                },
                new FixedAssetCategory
                {
                    Id = Guid.NewGuid(),
                    Code = "MOB",
                    Name = "Mobiliario y Equipo de Oficina",
                    AssetAccountCode = "1330",
                    AccumulatedDepreciationAccountCode = "1330.1",
                    DepreciationExpenseAccountCode = "5200",
                    UsefulLifeMonths = 60,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                },
                new FixedAssetCategory
                {
                    Id = Guid.NewGuid(),
                    Code = "INF",
                    Name = "Infraestructura y Mejoras",
                    AssetAccountCode = "1340",
                    AccumulatedDepreciationAccountCode = "1340.1",
                    DepreciationExpenseAccountCode = "5200",
                    UsefulLifeMonths = 120,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                }
            };

            await _context.FixedAssetCategories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();
        }

        // ======================================================
        // Módulo Configuración y Administración General — Semillas
        // ======================================================
        if (!await _context.Currencies.AnyAsync())
        {
            var currencies = new[]
            {
                new Currency
                {
                    Id = Guid.NewGuid(),
                    Code = "NIO",
                    Name = "Córdoba",
                    Symbol = "C$",
                    ExchangeRate = 1.0m,
                    IsDefault = true,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                },
                new Currency
                {
                    Id = Guid.NewGuid(),
                    Code = "USD",
                    Name = "Dólar estadounidense",
                    Symbol = "$",
                    ExchangeRate = 36.5m,
                    IsDefault = false,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow
                }
            };
            await _context.Currencies.AddRangeAsync(currencies);
            await _context.SaveChangesAsync();
        }

        if (!await _context.PricingThresholds.AnyAsync())
        {
            var thresholds = new[]
            {
                new PricingThreshold
                {
                    Id = Guid.NewGuid(),
                    LevelName = "SEMI MAYORISTA",
                    MinimumSubtotal = 10000.00m,
                    IsActive = true
                },
                new PricingThreshold
                {
                    Id = Guid.NewGuid(),
                    LevelName = "MAYORISTA",
                    MinimumSubtotal = 30000.00m,
                    IsActive = true
                }
            };
            await _context.PricingThresholds.AddRangeAsync(thresholds);
            await _context.SaveChangesAsync();
        }

        if (!await _context.SystemParameters.AnyAsync())
        {
            var parameters = new[]
            {
                new SystemParameter
                {
                    Id = Guid.NewGuid(),
                    Key = "MinimumInvoiceAmount",
                    Value = "350.00",
                    Description = "Monto mínimo total requerido para emitir una factura de venta o registrar un pedido de venta."
                }
            };
            await _context.SystemParameters.AddRangeAsync(parameters);
            await _context.SaveChangesAsync();
        }
    }

    private record TempProductData(
        string Code,
        string Name,
        string CategoryName,
        string Ue,
        int BoxFactor,
        decimal SemiUnit,
        decimal SemiBox,
        decimal RetailUnit,
        decimal RetailBox,
        decimal WholesaleUnit,
        decimal WholesaleBox,
        decimal? CostUnit = null,
        decimal? CostBox = null
    );

    private async Task ResetAndSeedNewCatalogAsync(Guid branchId)
    {
        System.Console.WriteLine("Iniciando actualización/siembra de catálogo de productos...");
        // 1. Obtener o crear categorías
        var catGalletas = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name == "Galletas") 
            ?? new Category { Id = Guid.NewGuid(), Name = "Galletas", Description = "Galletas y Waffers", CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
        var catCaramelos = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name == "Caramelos") 
            ?? new Category { Id = Guid.NewGuid(), Name = "Caramelos", Description = "Caramelos y Dulces", CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
        var catMalvaviscos = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name == "Malvaviscos") 
            ?? new Category { Id = Guid.NewGuid(), Name = "Malvaviscos", Description = "Malvaviscos y Masmelos", CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
        var catToallas = await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Name == "Toallas y Otros") 
            ?? new Category { Id = Guid.NewGuid(), Name = "Toallas y Otros", Description = "Toallas húmedas y aseo personal", CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };

        if (_context.Entry(catGalletas).State == EntityState.Detached) await _context.Categories.AddAsync(catGalletas);
        if (_context.Entry(catCaramelos).State == EntityState.Detached) await _context.Categories.AddAsync(catCaramelos);
        if (_context.Entry(catMalvaviscos).State == EntityState.Detached) await _context.Categories.AddAsync(catMalvaviscos);
        if (_context.Entry(catToallas).State == EntityState.Detached) await _context.Categories.AddAsync(catToallas);

        // 2. Obtener o crear marca genérica
        var brandGenerica = await _context.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Name == "Genérica") 
            ?? new Brand { Id = Guid.NewGuid(), Name = "Genérica", Description = "Marca Genérica", CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
        if (_context.Entry(brandGenerica).State == EntityState.Detached) await _context.Brands.AddAsync(brandGenerica);

        // 3. Obtener o crear Unidades de Medida
        var uomUnd = await _context.UnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Code == "UND") 
            ?? new UnitOfMeasure { Id = Guid.NewGuid(), Code = "UND", Name = "Unidad", CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
        if (_context.Entry(uomUnd).State == EntityState.Detached) await _context.UnitsOfMeasure.AddAsync(uomUnd);

        var uomCaja = await _context.UnitsOfMeasure.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Code == "CAJA") 
            ?? new UnitOfMeasure { Id = Guid.NewGuid(), Code = "CAJA", Name = "Caja", CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
        if (_context.Entry(uomCaja).State == EntityState.Detached) await _context.UnitsOfMeasure.AddAsync(uomCaja);

        // 4. Impuesto Exento
        var taxExento = await _context.Taxes.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Rate == 0) 
            ?? new Tax { Id = Guid.NewGuid(), Name = "Exento", Rate = 0.00m, CreatedBy = "System", CreatedOnUtc = DateTime.UtcNow };
        if (_context.Entry(taxExento).State == EntityState.Detached) await _context.Taxes.AddAsync(taxExento);

        await _context.SaveChangesAsync();

        // 5. Obtener los IDs de las bodegas para Casa Matriz
        var branchWarehouses = await _context.BranchWarehouses.Include(bw => bw.Warehouse).Where(bw => bw.BranchId == branchId).ToListAsync();
        var bgCM = branchWarehouses.FirstOrDefault(bw => bw.Warehouse.Name.Contains("General")) ?? branchWarehouses.FirstOrDefault();

        // 7. Lista de productos
        var productsData = new List<TempProductData>
        {
            // Galletas (38)
            new("GA001", "GALLETAS WAFFER AMORIS CHOCOLATE", "Galletas", "1/30", 30, 15.75m, 472.50m, 16.80m, 504.00m, 14.32m, 429.55m),
            new("GA002", "GALLETAS WAFFER AMORIS FRESA", "Galletas", "1/30", 30, 15.75m, 472.50m, 16.80m, 504.00m, 14.32m, 429.55m),
            new("GA003", "GALLETAS WAFFER AMORIS ESCURETO", "Galletas", "1/30", 30, 15.75m, 472.50m, 16.80m, 504.00m, 14.32m, 429.55m),
            new("GA004", "GALLETAS TRIO 4 CANDY CHOCOLATE", "Galletas", "1*24*12", 24, 56.25m, 1350.00m, 58.24m, 1397.65m, 56.25m, 1350.00m),
            new("GA005", "GALLETAS TRIO 4 CANDY FRESA", "Galletas", "1*24*12", 24, 56.25m, 1350.00m, 58.24m, 1397.65m, 56.25m, 1350.00m),
            new("GA006", "GALLETAS TRIO 4 CANDY LIMON", "Galletas", "1*24*12", 24, 58.24m, 1397.65m, 58.24m, 1397.65m, 56.25m, 1350.00m),
            new("GA007", "TWINNIES FRESA", "Galletas", "1*24*12", 24, 47.65m, 1143.53m, 49.39m, 1185.37m, 46.02m, 1104.55m),
            new("GA008", "TWINNIES CHOCO VAINILLA", "Galletas", "1*24*12", 24, 47.65m, 1143.53m, 49.39m, 1185.37m, 46.02m, 1104.55m),
            new("GA009", "TWINNIES LIMON", "Galletas", "1*24*12", 24, 47.65m, 1143.53m, 49.39m, 1185.37m, 46.02m, 1104.55m),
            new("GA010", "TWINNIES VAINILLA", "Galletas", "1*24*12", 24, 47.65m, 1143.53m, 49.39m, 1185.37m, 46.02m, 1104.55m),
            new("GA011", "TWINNIES CHOCO CHOCO", "Galletas", "1*24*12", 24, 47.65m, 1143.53m, 49.39m, 1185.37m, 46.02m, 1104.55m),
            new("GA012", "DISPENSADOR CHOCO GALLETIN CRISPY", "Galletas", "1*24*12", 24, 71.54m, 1716.92m, 74.40m, 1785.60m, 63.41m, 1521.82m),
            new("GA013", "GALLETA CANDY GLUCOSA CREMELO FRESA", "Galletas", "1*24*12", 24, 46.10m, 1106.34m, 47.85m, 1148.35m, 42.95m, 1030.91m),
            new("GA014", "GALLETA CANDY GLUCOSA CREMELO COCO", "Galletas", "1*24*12", 24, 46.10m, 1106.34m, 47.85m, 1148.35m, 42.95m, 1030.91m),
            new("GA015", "GALLETA CANDY GLUCOSA CREMELO LECHE", "Galletas", "1*24*12", 24, 46.10m, 1106.34m, 47.85m, 1148.35m, 42.95m, 1030.91m),
            new("GA016", "GALLETA AMORIS REDONDA 125GR. FRESA", "Galletas", "1/36", 36, 20.38m, 733.85m, 21.20m, 763.20m, 18.07m, 650.45m),
            new("GA017", "GALLETA AMORIS REDONDA 125GR. CHOCOLATE", "Galletas", "1/36", 36, 20.38m, 733.85m, 21.20m, 763.20m, 18.07m, 650.45m),
            new("GA018", "GALLETA AMORIS REDONDA 125GR. ESCURETO", "Galletas", "1/36", 36, 20.38m, 733.85m, 21.20m, 763.20m, 18.07m, 650.45m),
            new("GA019", "TARROS MD SUPER KRUNCH ORANGE", "Galletas", "1*6*60", 6, 351.22m, 2107.32m, 360.00m, 2160.00m, 327.27m, 1963.64m),
            new("GA020", "GALLETA JUMBO FRESA", "Galletas", "1*10*12", 10, 97.92m, 979.20m, 97.92m, 979.20m, 83.45m, 834.55m),
            new("GA021", "GALLETA JUMBO VAINILLA", "Galletas", "1*10*12", 10, 97.92m, 979.20m, 97.92m, 979.20m, 83.45m, 834.55m),
            new("GA022", "GALLETA JUMBO CHOCOLATE", "Galletas", "1*10*12", 10, 97.92m, 979.20m, 97.92m, 979.20m, 83.45m, 834.55m),
            new("GA023", "GALLETA KIDMAX FRESA", "Galletas", "1*24*12", 24, 48.13m, 1155.00m, 48.13m, 1155.00m, 39.38m, 945.00m),
            new("GA024", "GALLETA KIDMAX VAINILLA", "Galletas", "1*24*12", 24, 48.13m, 1155.00m, 48.13m, 1155.00m, 39.38m, 945.00m),
            new("GA025", "GALLETA KIDMAX CHOCOLATE", "Galletas", "1*24*12", 24, 48.13m, 1155.00m, 48.13m, 1155.00m, 39.38m, 945.00m),
            new("GA026", "TORTITAS ZOO ANIMADOS FRESA", "Galletas", "1*6*12", 6, 102.86m, 617.14m, 102.86m, 617.14m, 81.82m, 490.91m),
            new("GA027", "TORTITAS ZOO ANIMADOS VAINILLA", "Galletas", "1*6*12", 6, 102.86m, 617.14m, 102.86m, 617.14m, 81.82m, 490.91m),
            new("GA028", "TORTITAS ZOO ANIMADOS CHOCOLATE", "Galletas", "1*6*12", 6, 102.86m, 617.14m, 102.86m, 617.14m, 81.82m, 490.91m),
            new("GA029", "GALLETAS WAFFER ZOO ANIMADOS FRESA", "Galletas", "1*2*20", 2, 154.29m, 308.57m, 154.29m, 308.57m, 122.73m, 245.45m),
            new("GA030", "GALLETAS WAFFER ZOO ANIMADOS CHOCOLATE", "Galletas", "1*2*20", 2, 154.29m, 308.57m, 154.29m, 308.57m, 122.73m, 245.45m),
            new("GA031", "GALLETAS WAFFER AMORIS CODIGO SURTIDO", "Galletas", "1/30", 30, 15.75m, 472.50m, 16.80m, 504.00m, 14.32m, 429.55m),
            new("GA032", "GALLETA AMORIS REDONDA 125GR. CODIGO SURTIDO", "Galletas", "1/36", 36, 20.38m, 733.85m, 21.20m, 763.20m, 18.07m, 650.45m),
            new("GA033", "GALLETA TWINNIES CODIGO SURTIDO", "Galletas", "1*24*12", 24, 47.65m, 1143.53m, 49.39m, 1185.37m, 46.02m, 1104.55m),
            new("GA034", "GALLETA JUMBO CODIGO SURTIDO", "Galletas", "1*10*12", 10, 97.92m, 979.20m, 97.92m, 979.20m, 83.45m, 834.55m),
            new("GA035", "GALLETA KIDMAX SURTIDO", "Galletas", "1*24*12", 24, 48.13m, 1155.00m, 48.13m, 1155.00m, 39.38m, 945.00m),
            new("GA036", "TORTITAS ZOO ANIMADOS SURTIDO", "Galletas", "1*6*12", 6, 102.86m, 617.14m, 102.86m, 617.14m, 81.82m, 490.91m),
            new("GA037", "GALLETAS WAFFER ZOO ANIMADOS SURTIDO", "Galletas", "1*2*20", 2, 154.29m, 308.57m, 154.29m, 308.57m, 122.73m, 245.45m),
            new("GA038", "GALLETAS TRIO 4 CANDY SURTIDO", "Galletas", "1*24*12", 24, 56.25m, 1350.00m, 58.24m, 1397.65m, 56.25m, 1350.00m),

            // Caramelos (40)
            new("CA001", "DISPENSADOR CANDY CHISQUETAZO", "Caramelos", "1*12*24", 12, 144.00m, 1728.00m, 154.29m, 1851.43m, 122.73m, 1472.73m),
            new("CA002", "DISPENSADOR CANDY SUPLAY CANDY BRUSH", "Caramelos", "1*12*24", 12, 180.00m, 2160.00m, 192.86m, 2314.29m, 153.41m, 1840.91m),
            new("CA003", "DISPENSADOR YUGL GOMITA GUITARRA", "Caramelos", "1*12*24", 12, 207.60m, 2491.20m, 222.43m, 2669.14m, 176.93m, 2123.18m),
            new("CA004", "DISPENSADOR BON TON ROLL", "Caramelos", "1*24*30", 24, 120.00m, 2880.00m, 128.57m, 3085.71m, 102.27m, 2454.55m),
            new("CA005", "BOLSA DE BON TON", "Caramelos", "1*12*50", 12, 252.00m, 3024.00m, 270.00m, 3240.00m, 214.77m, 2577.27m),
            new("CA006", "BOLSA TEDDY BEAR 4", "Caramelos", "1*12*150", 12, 120.00m, 1440.00m, 128.57m, 1542.86m, 102.27m, 1227.27m),
            new("CA007", "TARROS NR 112 MIX TERMO CARAMELO", "Caramelos", "1*6*800", 6, 528.00m, 3168.00m, 660.00m, 3960.00m, 450.00m, 2700.00m),
            new("CA008", "DISPENSADOR SORPRESA DEL MUNDIAL 3D", "Caramelos", "1*20*30", 20, 151.20m, 3024.00m, 162.00m, 3240.00m, 128.86m, 2577.27m),
            new("CA009", "HERMETICO MD 198C KING EGG CHOCOLATE HERMETICO", "Caramelos", "1*6*60", 6, 528.00m, 3168.00m, 565.71m, 3394.29m, 450.00m, 2700.00m),
            new("CA010", "CANDY MINI GELATINA BOLSA GRANDE 1.5 KG.", "Caramelos", "1*8*100", 8, 126.00m, 1008.00m, 135.00m, 1080.00m, 107.39m, 859.09m),
            new("CA011", "BOLSA CANDY MINI GELATINA LAS DELICIA PEQUEÑA", "Caramelos", "1*40*20", 40, 25.20m, 1008.00m, 27.00m, 1080.00m, 21.48m, 859.09m),
            new("CA012", "DISPENSADOR CANDY SUPLAY CHUPON LUZ", "Caramelos", "1*12*24", 12, 156.00m, 1872.00m, 167.14m, 2005.71m, 132.95m, 1595.45m),
            new("CA013", "DISPENSADOR CANDY SUPLAY PALETA LUZ ANILLO", "Caramelos", "1*12*24", 12, 156.00m, 1872.00m, 167.14m, 2005.71m, 132.95m, 1595.45m),
            new("CA014", "DISPENSADOR CANDY CHOCO CRUCH CONO", "Caramelos", "1*12*20", 12, 144.00m, 1728.00m, 154.29m, 1851.43m, 122.73m, 1472.73m),
            new("CA015", "DISPENSADOR MD 233 KING EGG HUEVO EN CHOCOLATE", "Caramelos", "1*12*24", 12, 240.00m, 2880.00m, 257.14m, 3085.71m, 204.55m, 2454.55m),
            new("CA016", "TARRO CANDY SUPLAY MINI CHICLE TATOO", "Caramelos", "1*24*150", 24, 132.00m, 3168.00m, 123.75m, 2970.00m, 112.50m, 2700.00m),
            new("CA017", "BOLSA CANDY SUPLAY MINI CHICLE TATOO", "Caramelos", "1*24*150", 24, 120.00m, 2880.00m, 112.50m, 2700.00m, 102.27m, 2454.55m),
            new("CA018", "DISPENSADOR MD RF 2155-3 SLAY STITCH", "Caramelos", "1*16*6", 16, 708.00m, 11328.00m, 758.57m, 12137.14m, 603.41m, 9654.55m),
            new("CA019", "TARRO MD MINI KRIZZY HUEVO CELESTE", "Caramelos", "1*12*60", 12, 300.00m, 3600.00m, 321.43m, 3857.14m, 255.68m, 3068.18m),
            new("CA020", "TARRO MD MINI KRIZZY HUEVO ROJO", "Caramelos", "1*12*60", 12, 300.00m, 3600.00m, 321.43m, 3857.14m, 255.68m, 3068.18m),
            new("CA021", "DISPENSADOR PECCIN CHICLE BLONG NAPOLITANO", "Caramelos", "1*24*40", 24, 72.00m, 1728.00m, 77.14m, 1851.43m, 61.36m, 1472.73m),
            new("CA022", "BOLSA MD NR 47 CARAMELO MIX 3 LIBRAS", "Caramelos", "1*6", 6, 235.20m, 1411.20m, 252.00m, 1512.00m, 200.45m, 1202.73m, 166.67m, 1000.00m),
            new("CA023", "BOLSA MD NR 47 CARAMELO MIX 5 LIBRAS", "Caramelos", "1*6", 6, 384.00m, 2304.00m, 411.43m, 2468.57m, 327.27m, 1963.64m),
            new("CA024", "BOLSA MD NR 47 CARAMELO MIX 10 LIBRAS", "Caramelos", "1*4", 4, 720.00m, 2880.00m, 771.43m, 3085.71m, 613.64m, 2454.55m, 625.00m, 2500.00m),
            new("CA025", "DISPENSADOR MD CRISPY TWINS DOBLE ROLLO CHOCOLATE CAJA ANARANJADA", "Caramelos", "1*20*30", 20, 144.00m, 2880.00m, 154.29m, 3085.71m, 122.73m, 2454.55m),
            new("CA026", "DISPENSADOR MD CRISPY TWINS DOBLE ROLLO FRESA CAJA ROSADA", "Caramelos", "1*20*30", 20, 144.00m, 2880.00m, 154.29m, 3085.71m, 122.73m, 2454.55m),
            new("CA027", "DISPENSADOR MD CRISPY TWINS DOBLE ROLLO LECHE CAJA AZUL", "Caramelos", "1*20*30", 20, 144.00m, 2880.00m, 154.29m, 3085.71m, 122.73m, 2454.55m),
            new("CA028", "UNIDADES CANDY MINI BOLO SURTIDO NIÑA", "Caramelos", "1*100", 100, 47.52m, 4752.00m, 50.91m, 5091.43m, 40.50m, 4050.00m),
            new("CA029", "UNIDADES CANDY MINI BOLO SURTIDO NIÑO", "Caramelos", "1*100", 100, 47.52m, 4752.00m, 50.91m, 5091.43m, 40.50m, 4050.00m),
            new("CA030", "UNIDADES MD NR 168 CHOCOLATE MILKSTAR PANA", "Caramelos", "1*12*40", 6, 372.00m, 2232.00m, 398.57m, 2391.43m, 317.05m, 1902.27m, 258.33m, 1550.00m),
            new("CA031", "UNIDADES MD NR 168 CHOCOLATE MIL STAR PANA", "Caramelos", "1*6*40", 6, 372.00m, 2232.00m, 398.57m, 2391.43m, 317.05m, 1902.27m),
            new("CA032", "MD 423 SOUR CANDY SUPER STIMULANTE EXTREMO ACIDO", "Caramelos", "1*20*30", 20, 148.32m, 2966.40m, 158.91m, 3178.29m, 126.41m, 2528.18m, 82.50m, 1650.00m),
            new("CA033", "TARRO MD NTC-321 OJO EN TARRO CON TRONADOR", "Caramelos", "1*8*60", 8, 297.00m, 2376.00m, 318.21m, 2545.71m, 253.13m, 2025.00m),
            new("CA034", "TARRO FRESH OLIVE BALL BUBBLEGUM", "Caramelos", "1*12*150", 8, 297.18m, 2377.44m, 187.71m, 1501.71m, 223.98m, 1791.82m),
            new("CA035", "TARRO RUGBY BALL BUBBLE GUM", "Caramelos", "1*24*220", 8, 297.36m, 2378.88m, 90.00m, 720.00m, 214.77m, 1718.18m),
            new("CA036", "TARRO SUPER TARZAN BUBBLE GUM", "Caramelos", "1*12*250", 8, 297.54m, 2380.32m, 102.86m, 822.86m, 122.73m, 981.82m),
            new("CA037", "KOBA TARRO FRESH OLIVE BALL BUBBLEGUM", "Caramelos", "1*24*100", 8, 297.72m, 2381.76m, 73.93m, 591.43m, 176.42m, 1411.36m),
            new("CA038", "CARAMELO CRYSTAL", "Caramelos", "1*8*200", 8, 297.90m, 2383.20m, 144.64m, 1157.14m, 115.06m, 920.45m),
            new("CA039", "BOLSA JUICE BURST", "Caramelos", "1*12*150", 8, 298.08m, 2384.64m, 154.29m, 1234.29m, 184.09m, 1472.73m),
            new("CA040", "YOHAN BALL BUBBLEGUM", "Caramelos", "1*12*250", 8, 298.26m, 2386.08m, 205.71m, 1645.71m, 245.45m, 1963.64m),
            new("CA041", "BOLSA FRUTZUCOS", "Caramelos", "1*30*20", 30, 63.36m, 1900.80m, 67.89m, 2036.57m, 54.00m, 1620.00m, 54.00m, 1620.00m),

            // Malvaviscos (8)
            new("MA001", "BOLSA CANDY SUPLAY MALVAVISCO SUPER MINI MINO", "Malvaviscos", "1*8*70", 8, 252.00m, 2016.00m, 270.00m, 2160.00m, 214.77m, 1718.18m),
            new("MA002", "BOLSA MALVAVISCO MALVA MUFFIN", "Malvaviscos", "1*12*24", 12, 132.00m, 1584.00m, 141.43m, 1697.14m, 112.50m, 1350.00m),
            new("MA003", "BOLSA MALVAVISCO ICE CREAM PLUS", "Malvaviscos", "1*12*40", 12, 144.00m, 1728.00m, 154.29m, 1851.43m, 122.73m, 1472.73m),
            new("MA004", "BOLSA MALVAVISCO ICE CREAM ACIDULADO", "Malvaviscos", "1*12*24", 12, 144.00m, 1728.00m, 154.29m, 1851.43m, 122.73m, 1472.73m),
            new("MA005", "BOLSA MALVAVISCO YETI MALLOW", "Malvaviscos", "1*12*24", 12, 162.00m, 1944.00m, 173.57m, 2082.86m, 138.07m, 1656.82m),
            new("MA006", "BOLSA MALVAVISCO RELLENO TRI PACK", "Malvaviscos", "1*15*24*3 UNIDADES", 15, 117.12m, 1756.80m, 125.49m, 1882.29m, 99.82m, 1497.27m),
            new("MA007", "BOLSA MALVAVISCO 3D", "Malvaviscos", "1*16*30", 16, 97.20m, 1555.20m, 104.14m, 1666.29m, 82.84m, 1325.45m),
            new("MA008", "DISPENSADOR MALVAVISCO MALVA POP", "Malvaviscos", "1*12*30", 12, 192.00m, 2304.00m, 205.71m, 2468.57m, 163.64m, 1963.64m),
            new("MA009", "MALVAVISCO DIPA", "Malvaviscos", "1*20*30", 20, 180.00m, 3600.00m, 192.86m, 3857.14m, 153.41m, 3068.18m, 125.00m, 2500.00m),
            new("MA010", "COTTOM CANDY COLORFULL ROLLS", "Malvaviscos", "1*20*30", 20, 158.40m, 3168.00m, 169.71m, 3394.29m, 135.00m, 2700.00m, 110.00m, 2200.00m),
            new("MA011", "RAINBOW LONG MARSHMALLOW", "Malvaviscos", "1*8*65", 8, 289.80m, 2318.40m, 310.50m, 2484.00m, 246.99m, 1975.91m, 201.25m, 1610.00m),
            new("MA012", "TWINST MARSHMALLOW", "Malvaviscos", "1*8*180", 8, 198.00m, 1584.00m, 212.14m, 1697.14m, 168.75m, 1350.00m, 137.50m, 1100.00m),

            // Toallas y Otros (12)
            new("TA001", "TOALLAS HUMEDAS FAMILYS ROBELLY CELESTE 96 HOJAS", "Toallas y Otros", "1*12", 12, 63.53m, 762.35m, 63.53m, 762.35m, 61.36m, 736.36m),
            new("TA002", "TOALLAS HUMEDAS MIDDY BEAR 80 HOJAS", "Toallas y Otros", "1*12", 12, 68.82m, 825.88m, 73.13m, 877.50m, 66.48m, 797.73m),
            new("TA003", "PAQUETE DE TOALLITA HUMEDA MIDDY BEAR 120 HOJAS", "Toallas y Otros", "1*12", 12, 72.00m, 864.00m, 76.50m, 918.00m, 69.55m, 834.55m),
            new("TA004", "UNIDAD JN203 TOALLITA COMPRIMIDA MIDDY BEAR", "Toallas y Otros", "1*30*10", 30, 79.71m, 2391.43m, 79.71m, 2391.43m, 63.41m, 1902.27m),
            new("TA005", "TOALLA HUMEDA SIPPACK MIDDY BEAR", "Toallas y Otros", "1*20*8", 20, 57.86m, 1157.14m, 57.86m, 1157.14m, 46.02m, 920.45m),
            new("TA006", "PAPEL HIGIENICO ROBELLY 1620 HOJAS", "Toallas y Otros", "1/24", 24, 23.82m, 571.76m, 23.82m, 571.76m, 23.01m, 552.27m),
            new("TA007", "BOLSON DE PAÑALES CALSON OSITO -TALLAS; S,M,L,XL,XXL,3XL Y 4XL.", "Toallas y Otros", "1/4", 4, 594.51m, 2378.05m, 609.38m, 2437.50m, 541.67m, 2166.67m, 487.50m, 1950.00m),
            new("TA008", "BOLSON DE PAÑALES PEGA PEGA OSITO -TALLAS; S,M,L,XL,XXL Y XXXL.", "Toallas y Otros", "1/4", 4, 594.51m, 2378.05m, 609.38m, 2437.50m, 541.67m, 2166.67m, 487.50m, 1950.00m),
            new("TA009", "PAQUETE DE PAÑAL ADULTO MOMMY BEAR -TALLAS; M,L,XL.", "Toallas y Otros", "1*8*10", 8, 321.43m, 2571.43m, 321.43m, 2571.43m, 255.68m, 2045.45m, 225.00m, 1800.00m),
            new("TA010", "TALCO SURTIDO MIDDY BEAR 635GR.", "Toallas y Otros", "1*12", 12, 77.14m, 925.71m, 77.14m, 925.71m, 61.36m, 736.36m),
            new("TO011", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON -TALLAS; M,L,XL,XXL,XXXL,4XL,5XL,6XL", "Toallas y Otros", "1*4*50", 4, 594.51m, 2378.05m, 609.38m, 2437.50m, 541.67m, 2166.67m, 487.50m, 1950.00m),
            new("TO012", "PAÑAL LUCAS SUPER SET.TALLAS; S,M,L,XL,XXL", "Toallas y Otros", "1*4*50", 4, 304.88m, 1219.51m, 312.50m, 1250.00m, 277.78m, 1111.11m, 250.00m, 1000.00m)
        };

        foreach (var data in productsData)
        {
            var cat = data.CategoryName switch
            {
                "Galletas" => catGalletas,
                "Caramelos" => catCaramelos,
                "Malvaviscos" => catMalvaviscos,
                _ => catToallas
            };

            var existingProduct = await _context.Products
                .Include(p => p.Presentations)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.InternalCode == data.Code);

            if (existingProduct != null)
            {
                existingProduct.Name = data.Name;
                existingProduct.Description = $"{data.Name} (U/E: {data.Ue})";
                existingProduct.CategoryId = cat.Id;
                existingProduct.BrandId = brandGenerica.Id;
                existingProduct.IsActive = true;
                existingProduct.CurrentCost = data.CostUnit ?? (data.WholesaleUnit * 0.85m);

                foreach (var presentation in existingProduct.Presentations)
                {
                    if (presentation.IsBaseUnit || presentation.ConversionFactor == 1.0000m)
                    {
                        presentation.RetailPrice = data.RetailUnit;
                        presentation.SemiWholesalePrice = data.SemiUnit;
                        presentation.WholesalePrice = data.WholesaleUnit;
                        presentation.Cost = data.CostUnit ?? (data.WholesaleUnit * 0.85m);
                    }
                    else
                    {
                        presentation.ConversionFactor = (decimal)data.BoxFactor;
                        presentation.RetailPrice = data.RetailBox;
                        presentation.SemiWholesalePrice = data.SemiBox;
                        presentation.WholesalePrice = data.WholesaleBox;
                        presentation.Cost = data.CostBox ?? (data.WholesaleBox * 0.85m);
                    }
                }
                _context.Products.Update(existingProduct);
            }
            else
            {
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    InternalCode = data.Code,
                    Name = data.Name,
                    Description = $"{data.Name} (U/E: {data.Ue})",
                    ProductType = ProductType.Physical,
                    ProductStatus = ProductStatus.Active,
                    TrackInventory = true,
                    RequiresSerialNumber = false,
                    RequiresBatchControl = false,
                    CategoryId = cat.Id,
                    BrandId = brandGenerica.Id,
                    DefaultUnitOfMeasureId = uomUnd.Id,
                    CurrentCost = data.CostUnit ?? (data.WholesaleUnit * 0.85m),
                    CreatedBy = "System",
                    CreatedOnUtc = DateTime.UtcNow,
                    TaxId = taxExento.Id,
                    IsCatalogVisible = true,
                    IsActive = true
                };

                var presentationUnit = new ProductPresentation
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    UnitOfMeasureId = uomUnd.Id,
                    Name = "Unidad",
                    ConversionFactor = 1.0000m,
                    Barcode = data.Code + "U",
                    Cost = data.CostUnit ?? (data.WholesaleUnit * 0.85m),
                    RetailPrice = data.RetailUnit,
                    SemiWholesalePrice = data.SemiUnit,
                    WholesalePrice = data.WholesaleUnit,
                    IsBaseUnit = true,
                    IsDefaultSalePresentation = true,
                    IsActive = true
                };
                product.Presentations.Add(presentationUnit);

                var presentationBox = new ProductPresentation
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    UnitOfMeasureId = uomCaja.Id,
                    Name = "Caja",
                    ConversionFactor = (decimal)data.BoxFactor,
                    Barcode = data.Code + "C",
                    Cost = data.CostBox ?? (data.WholesaleBox * 0.85m),
                    RetailPrice = data.RetailBox,
                    SemiWholesalePrice = data.SemiBox,
                    WholesalePrice = data.WholesaleBox,
                    IsBaseUnit = false,
                    IsDefaultSalePresentation = false,
                    IsActive = true
                };
                product.Presentations.Add(presentationBox);

                await _context.Products.AddAsync(product);

                // Existencias Iniciales (100 Cajas de cada producto = 100 * BoxFactor Unidades)
                if (bgCM != null)
                {
                    decimal initialStock = 100m * data.BoxFactor;
                    var inventory = new Inventory
                    {
                        Id = Guid.NewGuid(),
                        BranchWarehouseId = bgCM.Id,
                        ProductId = product.Id,
                        PhysicalStock = initialStock,
                        ReservedStock = 0.0000m,
                        CommittedStock = 0.0000m,
                        CreatedBy = "System",
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    await _context.Inventories.AddAsync(inventory);

                    // Movimiento inicial de inventario
                    var movement = new InventoryMovement
                    {
                        Id = Guid.NewGuid(),
                        MovementNumber = $"MOV-INIT-{data.Code}",
                        MovementType = MovementType.Entry,
                        ToBranchWarehouseId = bgCM.Id,
                        ReferenceDocument = "CARGA-INICIAL-CATALOGO",
                        Notes = $"Carga de existencias iniciales para {data.Name}",
                        MovementDate = DateTime.UtcNow,
                        CreatedBy = "System",
                        CreatedOnUtc = DateTime.UtcNow
                    };

                    movement.Details.Add(new InventoryMovementDetail
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Quantity = 100m,
                        UnitOfMeasureId = uomCaja.Id,
                        ProductPresentationId = presentationBox.Id,
                        ConversionFactor = (decimal)data.BoxFactor,
                        QuantityInBaseUnit = initialStock,
                        CreatedBy = "System",
                        CreatedOnUtc = DateTime.UtcNow
                    });

                    await _context.InventoryMovements.AddAsync(movement);
                }
            }
        }

        await _context.SaveChangesAsync();
        System.Console.WriteLine($"Catálogo de productos actualizado/sembrado con éxito. Total productos procesados: {productsData.Count}");
    }
}
