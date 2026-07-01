using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AccountName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.CurrentBalance)
            .HasPrecision(18, 4);

        builder.Property(x => x.AccountingAccountCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Relaciones
        builder.HasOne(x => x.Bank)
            .WithMany(x => x.BankAccounts)
            .HasForeignKey(x => x.BankId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index on AccountNumber per Bank
        builder.HasIndex(x => new { x.BankId, x.AccountNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Global query filter
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Check constraint CurrentBalance >= 0
        builder.ToTable(tb => tb.HasCheckConstraint("CK_BankAccounts_CurrentBalance_NonNegative", "[CurrentBalance] >= 0"));
    }
}
