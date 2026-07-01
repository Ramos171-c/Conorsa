using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class ProductPriceHistoryConfiguration : IEntityTypeConfiguration<ProductPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductPriceHistory> builder)
    {
        builder.ToTable("ProductPriceHistories");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OldRetailPrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.NewRetailPrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.OldSemiWholesalePrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.NewSemiWholesalePrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.OldWholesalePrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.NewWholesalePrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.OldCost)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.NewCost)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.ChangedBy)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Reason)
            .HasMaxLength(500);

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft Delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
