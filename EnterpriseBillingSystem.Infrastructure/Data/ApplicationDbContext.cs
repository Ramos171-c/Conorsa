using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Tax> Taxes => Set<Tax>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductPriceHistory> ProductPriceHistories => Set<ProductPriceHistory>();
    public DbSet<ProductPresentation> ProductPresentations => Set<ProductPresentation>();
    public DbSet<BranchProduct> BranchProducts => Set<BranchProduct>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<BranchWarehouse> BranchWarehouses => Set<BranchWarehouse>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<InventoryMovementDetail> InventoryMovementDetails => Set<InventoryMovementDetail>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    
    // Módulo Clientes
    public DbSet<CustomerCategory> CustomerCategories => Set<CustomerCategory>();
    public DbSet<CustomerPricingProfile> CustomerPricingProfiles => Set<CustomerPricingProfile>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<CustomerPhone> CustomerPhones => Set<CustomerPhone>();
    public DbSet<CustomerEmail> CustomerEmails => Set<CustomerEmail>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<Route> Routes => Set<Route>();

    // Módulo Compras
    public DbSet<SupplierCategory> SupplierCategories => Set<SupplierCategory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails => Set<PurchaseOrderDetail>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptDetail> PurchaseReceiptDetails => Set<PurchaseReceiptDetail>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceDetail> PurchaseInvoiceDetails => Set<PurchaseInvoiceDetail>();

    // Módulo Ventas / Facturación
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderDetail> SalesOrderDetails => Set<SalesOrderDetail>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceDetail> SalesInvoiceDetails => Set<SalesInvoiceDetail>();
    public DbSet<SystemParameter> SystemParameters => Set<SystemParameter>();

    // Módulo Caja
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();

    // Módulo Cuentas por Cobrar
    public DbSet<AccountsReceivable> AccountsReceivables => Set<AccountsReceivable>();
    public DbSet<AccountsReceivablePayment> AccountsReceivablePayments => Set<AccountsReceivablePayment>();

    // Módulo Cuentas por Pagar
    public DbSet<AccountsPayable> AccountsPayables => Set<AccountsPayable>();
    public DbSet<AccountsPayablePayment> AccountsPayablePayments => Set<AccountsPayablePayment>();

    // Módulo Configuración y Administración General
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<PricingThreshold> PricingThresholds => Set<PricingThreshold>();
    public DbSet<SalesGoal> SalesGoals => Set<SalesGoal>();

    // Módulo Contabilidad General
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryDetail> JournalEntryDetails => Set<JournalEntryDetail>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();

    // Módulo Tesorería y Bancos
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();

    // Módulo Activos Fijos
    public DbSet<FixedAssetCategory> FixedAssetCategories => Set<FixedAssetCategory>();
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<FixedAssetTransaction> FixedAssetTransactions => Set<FixedAssetTransaction>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IGlobalAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId ?? "System";
                    entry.Entity.CreatedOnUtc = DateTime.UtcNow;
                    if (entry.Entity is IAuditable auditable)
                    {
                        auditable.BranchId ??= _currentUserService.BranchId;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = _currentUserService.UserId ?? "System";
                    entry.Entity.LastModifiedOnUtc = DateTime.UtcNow;
                    break;
            }
        }

        await ProcessAutoSoldOutAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessAutoSoldOutAsync(CancellationToken cancellationToken)
    {
        var productIds = ChangeTracker.Entries<Inventory>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Select(e => e.Entity.ProductId)
            .Distinct()
            .ToList();

        if (!productIds.Any()) return;

        foreach (var productId in productIds)
        {
            var product = await Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
            if (product != null && product.AutoMarkSoldOut)
            {
                var dbItems = await Inventories.AsNoTracking()
                    .Where(i => i.ProductId == productId && !i.IsDeleted)
                    .ToListAsync(cancellationToken);

                var stockDict = dbItems.ToDictionary(i => i.Id, i => i.PhysicalStock);

                foreach (var entry in ChangeTracker.Entries<Inventory>().Where(e => e.Entity.ProductId == productId))
                {
                    if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    {
                        stockDict[entry.Entity.Id] = entry.Entity.PhysicalStock;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        stockDict.Remove(entry.Entity.Id);
                    }
                }

                var finalTotalStock = stockDict.Values.Sum();
                var shouldBeSoldOut = finalTotalStock <= 0;

                if (product.IsSoldOut != shouldBeSoldOut)
                {
                    product.IsSoldOut = shouldBeSoldOut;
                    product.SoldOutAt = shouldBeSoldOut ? DateTime.UtcNow : null;
                    product.SoldOutBy = shouldBeSoldOut ? (_currentUserService.UserId ?? "System (Auto)") : null;
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Personalizar nombres de tablas de ASP.NET Core Identity
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // 2. Configurar la entidad Branch (Sucursal)
        builder.Entity<Branch>(entity =>
        {
            entity.HasIndex(b => b.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasQueryFilter(b => !b.IsDeleted);
        });

        // 3. Configurar la entidad ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(u => u.NormalizedUserName).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasIndex(u => u.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL AND [IsDeleted] = 0");
            
            entity.Property(u => u.Cedula).HasMaxLength(50);
            entity.Property(u => u.Address).HasMaxLength(500);
            entity.Property(u => u.Municipality).HasMaxLength(100);
            entity.Property(u => u.City).HasMaxLength(100);
            entity.Property(u => u.EmergencyContactName).HasMaxLength(200);
            entity.Property(u => u.EmergencyContactPhone).HasMaxLength(50);
            entity.Property(u => u.RouteId).IsRequired(false);
            
            entity.HasOne(u => u.Route)
                .WithMany()
                .HasForeignKey(u => u.RouteId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(u => u.Cedula).IsUnique().HasFilter("[Cedula] IS NOT NULL AND [IsDeleted] = 0");

            // Relación con Sucursal por defecto (DefaultBranch)
            entity.HasOne(u => u.DefaultBranch)
                .WithMany()
                .HasForeignKey(u => u.DefaultBranchId)
                .OnDelete(DeleteBehavior.Restrict); // Evitar cascading circular

            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        // 4. Configurar la entidad ApplicationRole
        builder.Entity<ApplicationRole>(entity =>
        {
            entity.HasIndex(r => r.NormalizedName).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasQueryFilter(r => !r.IsDeleted);
        });

        // 5. Configurar la entidad Permission (Permisos RBAC)
        builder.Entity<Permission>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        // 6. Configurar la tabla intermedia UserBranch (UsuarioSucursales)
        builder.Entity<UserBranch>(entity =>
        {
            entity.HasKey(ub => new { ub.UserId, ub.BranchId });

            entity.HasOne(ub => ub.User)
                .WithMany(u => u.UserBranches)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ub => ub.Branch)
                .WithMany()
                .HasForeignKey(ub => ub.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. Configurar la entidad RefreshToken
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.Token).IsUnique();
            
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 8. Relación Roles y Permisos (RolePermissions muchos a muchos)
        builder.Entity<ApplicationRole>()
            .HasMany(r => r.Permissions)
            .WithMany()
            .UsingEntity<object>(
                "RolePermissions",
                l => l.HasOne<Permission>().WithMany().HasForeignKey("PermissionId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne<ApplicationRole>().WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("RoleId", "PermissionId");
                });

        // 9. Aplicar configuraciones externas (Módulo Productos)
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
