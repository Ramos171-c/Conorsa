using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.InternalCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.ImagePath)
            .HasMaxLength(500);

        builder.Property(p => p.MinimumStock)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        builder.Property(p => p.IsCatalogVisible)
            .HasDefaultValue(true);

        builder.Property(p => p.IsSoldOut)
            .HasDefaultValue(false);

        builder.Property(p => p.IsFavorite)
            .HasDefaultValue(false);

        builder.Property(p => p.FavoriteOrder)
            .HasDefaultValue(0);

        builder.Property(p => p.AllowPromotions)
            .HasDefaultValue(true);

        builder.Property(p => p.HighlightInCatalog)
            .HasDefaultValue(false);

        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500);

        builder.Property(p => p.CatalogBadge)
            .HasMaxLength(50);

        builder.Property(p => p.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(p => p.AutoMarkSoldOut)
            .HasDefaultValue(true);

        builder.Property(p => p.SoldOutBy)
            .HasMaxLength(100);

        builder.Property(p => p.ProductType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.ProductStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.CurrentCost)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        // SQL Check Constraints
        builder.ToTable(p => p.HasCheckConstraint("CK_Products_ProductType", "[ProductType] IN (1, 2, 3)"));
        builder.ToTable(p => p.HasCheckConstraint("CK_Products_ProductStatus", "[ProductStatus] IN (1, 2, 3, 4)"));
        builder.ToTable(p => p.HasCheckConstraint("CK_Products_Service_Flags", "[ProductType] <> 2 OR ([TrackInventory] = 0 AND [RequiresSerialNumber] = 0 AND [RequiresBatchControl] = 0)"));
        builder.ToTable(p => p.HasCheckConstraint("CK_Products_CurrentCost", "[CurrentCost] >= 0.0000"));

        // Relaciones
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.DefaultUnitOfMeasure)
            .WithMany()
            .HasForeignKey(p => p.DefaultUnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Tax)
            .WithMany(t => t.Products)
            .HasForeignKey(p => p.TaxId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices Únicos Filtrados
        builder.HasIndex(p => p.InternalCode)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Índices de búsquedas en FKs
        builder.HasIndex(p => p.CategoryId)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => p.BrandId)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => p.DefaultUnitOfMeasureId)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(p => p.TaxId)
            .HasFilter("[IsDeleted] = 0");

        // Soft Delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
