using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class AccountsPayableConfiguration : IEntityTypeConfiguration<AccountsPayable>
{
    public void Configure(EntityTypeBuilder<AccountsPayable> builder)
    {
        builder.ToTable("AccountsPayables");

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
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice único en PurchaseInvoiceId
        builder.HasIndex(x => x.PurchaseInvoiceId)
            .IsUnique();

        // Restricciones Check de integridad financiera
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_AccountsPayable_OriginalAmount_Positive", "[OriginalAmount] >= 0");
            tb.HasCheckConstraint("CK_AccountsPayable_PaidAmount_Positive", "[PaidAmount] >= 0");
            tb.HasCheckConstraint("CK_AccountsPayable_CurrentBalance_Positive", "[CurrentBalance] >= 0");
            tb.HasCheckConstraint("CK_AccountsPayable_Balance_Coherence", "[CurrentBalance] = [OriginalAmount] - [PaidAmount]");
            tb.HasCheckConstraint("CK_AccountsPayable_PaidAmount_Limit", "[PaidAmount] <= [OriginalAmount]");
        });

        // Filtro de borrado lógico (Soft Delete)
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
