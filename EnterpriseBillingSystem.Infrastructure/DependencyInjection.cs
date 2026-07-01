using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Infrastructure.Data;
using EnterpriseBillingSystem.Infrastructure.Repositories;
using EnterpriseBillingSystem.Infrastructure.Services;
using EnterpriseBillingSystem.Infrastructure.Identity;

namespace EnterpriseBillingSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurar JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Configurar DbContext con SqlServer
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Configurar ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Registrar Repositorio Genérico y Unit of Work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Registrar Repositorios Personalizados (Módulo Productos)
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
        services.AddScoped<ITaxRepository, TaxRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductPresentationRepository, ProductPresentationRepository>();

        // Registrar Repositorios Personalizados (Módulo Inventario)
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();

        // Registrar Repositorios Personalizados (Módulo Clientes)
        services.AddScoped<ICustomerCategoryRepository, CustomerCategoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // Registrar Repositorios Personalizados (Módulo Compras)
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IPurchaseReceiptRepository, PurchaseReceiptRepository>();
        services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();

        // Registrar Repositorios Personalizados (Módulo Ventas / Facturación)
        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();

        // Registrar Repositorios Personalizados (Módulo Caja)
        services.AddScoped<ICashRegisterRepository, CashRegisterRepository>();
        services.AddScoped<ICashSessionRepository, CashSessionRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IAccountsReceivableRepository, AccountsReceivableRepository>();
        services.AddScoped<IAccountsPayableRepository, AccountsPayableRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
        services.AddScoped<IBankRepository, BankRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<IBankTransactionRepository, BankTransactionRepository>();
        services.AddScoped<IBankReconciliationRepository, BankReconciliationRepository>();

        // Registrar Repositorios Personalizados (Módulo Activos Fijos)
        services.AddScoped<IFixedAssetCategoryRepository, FixedAssetCategoryRepository>();
        services.AddScoped<IFixedAssetRepository, FixedAssetRepository>();
        services.AddScoped<IFixedAssetTransactionRepository, FixedAssetTransactionRepository>();

        // Registrar Servicios
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDbInitializer, DbInitializer>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        return services;
    }
}
