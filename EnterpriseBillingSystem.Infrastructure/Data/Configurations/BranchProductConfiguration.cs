using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class BranchProductConfiguration : IEntityTypeConfiguration<BranchProduct>
{
    public void Configure(EntityTypeBuilder<BranchProduct> builder)
    {
        builder.ToTable("BranchProducts");

        builder.HasKey(bp => new { bp.BranchId, bp.ProductId });

        builder.Property(bp => bp.LocalSalePrice)
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(bp => bp.MinSalePrice)
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(bp => bp.MaxDiscountPercentage)
            .HasPrecision(5, 2)
            .IsRequired(false);

        builder.Property(bp => bp.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // SQL constraints
        builder.ToTable(bp => bp.HasCheckConstraint("CK_BranchProducts_LocalSalePrice", "[LocalSalePrice] IS NULL OR [LocalSalePrice] >= 0.0000"));
        builder.ToTable(bp => bp.HasCheckConstraint("CK_BranchProducts_MinSalePrice", "[MinSalePrice] IS NULL OR [MinSalePrice] >= 0.0000"));
        builder.ToTable(bp => bp.HasCheckConstraint("CK_BranchProducts_MaxDiscountPercentage", "[MaxDiscountPercentage] IS NULL OR ([MaxDiscountPercentage] >= 0.00 AND [MaxDiscountPercentage] <= 100.00)"));
        builder.ToTable(bp => bp.HasCheckConstraint("CK_BranchProducts_Prices_Coherence", "[LocalSalePrice] IS NULL OR [MinSalePrice] IS NULL OR [LocalSalePrice] >= [MinSalePrice]"));

        // Relaciones
        builder.HasOne(bp => bp.Branch)
            .WithMany()
            .HasForeignKey(bp => bp.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bp => bp.Product)
            .WithMany(p => p.BranchProducts)
            .HasForeignKey(bp => bp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
