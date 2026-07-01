using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class FixedAssetTransactionConfiguration : IEntityTypeConfiguration<FixedAssetTransaction>
{
    public void Configure(EntityTypeBuilder<FixedAssetTransaction> builder)
    {
        builder.ToTable("FixedAssetTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 4);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        // Relaciones
        builder.HasOne(x => x.FixedAsset)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.FixedAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.JournalEntry)
            .WithMany()
            .HasForeignKey(x => x.JournalEntryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.ToTable(tb =>
            tb.HasCheckConstraint("CK_FixedAssetTransactions_Amount_Positive", "[Amount] > 0"));
    }
}
