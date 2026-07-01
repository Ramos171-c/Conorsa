using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class FixedAssetCategoryConfiguration : IEntityTypeConfiguration<FixedAssetCategory>
{
    public void Configure(EntityTypeBuilder<FixedAssetCategory> builder)
    {
        builder.ToTable("FixedAssetCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AssetAccountCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AccumulatedDepreciationAccountCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DepreciationExpenseAccountCode)
            .IsRequired()
            .HasMaxLength(50);

        // Unique index on Code with soft-delete filter
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
