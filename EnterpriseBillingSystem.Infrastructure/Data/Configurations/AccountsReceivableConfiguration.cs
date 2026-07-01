using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class AccountsReceivableConfiguration : IEntityTypeConfiguration<AccountsReceivable>
{
    public void Configure(EntityTypeBuilder<AccountsReceivable> builder)
    {
        builder.ToTable("AccountsReceivables");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.OriginalAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.PaidAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.CurrentBalance)
            .HasPrecision(18, 4);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Relaciones
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalesInvoice)
            .WithMany()
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice único en SalesInvoiceId
        builder.HasIndex(x => x.SalesInvoiceId)
            .IsUnique();

        // Restricciones Check de integridad financiera
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_AccountsReceivable_OriginalAmount_Positive", "[OriginalAmount] >= 0");
            tb.HasCheckConstraint("CK_AccountsReceivable_PaidAmount_Positive", "[PaidAmount] >= 0");
            tb.HasCheckConstraint("CK_AccountsReceivable_CurrentBalance_Positive", "[CurrentBalance] >= 0");
            tb.HasCheckConstraint("CK_AccountsReceivable_Balance_Coherence", "[CurrentBalance] = [OriginalAmount] - [PaidAmount]");
            tb.HasCheckConstraint("CK_AccountsReceivable_PaidAmount_Limit", "[PaidAmount] <= [OriginalAmount]");
        });

        // Filtro de borrado lógico (Soft Delete)
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
