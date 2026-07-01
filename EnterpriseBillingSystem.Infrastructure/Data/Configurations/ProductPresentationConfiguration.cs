using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class ProductPresentationConfiguration : IEntityTypeConfiguration<ProductPresentation>
{
    public void Configure(EntityTypeBuilder<ProductPresentation> builder)
    {
        builder.ToTable("ProductPresentations");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Barcode)
            .HasMaxLength(100);

        builder.Property(p => p.ConversionFactor)
            .HasPrecision(18, 4)
            .HasDefaultValue(1.0000);

        builder.Property(p => p.Cost)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        builder.Property(p => p.RetailPrice)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        builder.Property(p => p.SemiWholesalePrice)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        builder.Property(p => p.WholesalePrice)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        builder.Property(p => p.IsBaseUnit)
            .HasDefaultValue(false);

        builder.Property(p => p.IsDefaultSalePresentation)
            .HasDefaultValue(false);

        builder.Property(p => p.AllowPurchase)
            .HasDefaultValue(true);

        builder.Property(p => p.AllowSale)
            .HasDefaultValue(true);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(p => p.Product)
            .WithMany(prod => prod.Presentations)
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(p => p.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.ProductId);
        
        builder.HasIndex(p => new { p.ProductId, p.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        
        builder.HasIndex(p => p.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");

        // Enforce exactly one default sale presentation per product active
        builder.HasIndex(p => new { p.ProductId, p.IsDefaultSalePresentation })
            .IsUnique()
            .HasFilter("[IsDefaultSalePresentation] = 1 AND [IsDeleted] = 0");

        // Soft Delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
