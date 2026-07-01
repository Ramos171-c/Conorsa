using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.ToTable("FixedAssets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssetNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.AcquisitionCost)
            .HasPrecision(18, 4);

        builder.Property(x => x.ResidualValue)
            .HasPrecision(18, 4);

        builder.Property(x => x.AccumulatedDepreciation)
            .HasPrecision(18, 4);

        builder.Property(x => x.CurrentBookValue)
            .HasPrecision(18, 4);

        builder.Property(x => x.Location)
            .HasMaxLength(200);

        builder.Property(x => x.SerialNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Relaciones
        builder.HasOne(x => x.Category)
            .WithMany(x => x.FixedAssets)
            .HasForeignKey(x => x.FixedAssetCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index on AssetNumber
        builder.HasIndex(x => x.AssetNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(x => !x.IsDeleted);

        // Check constraints financieros
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_FixedAssets_AcquisitionCost_NonNegative", "[AcquisitionCost] >= 0");
            tb.HasCheckConstraint("CK_FixedAssets_ResidualValue_NonNegative", "[ResidualValue] >= 0");
            tb.HasCheckConstraint("CK_FixedAssets_CurrentBookValue_NonNegative", "[CurrentBookValue] >= 0");
            tb.HasCheckConstraint("CK_FixedAssets_AccumulatedDepreciation_NonNegative", "[AccumulatedDepreciation] >= 0");
        });
    }
}
