using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class AccountsReceivablePaymentConfiguration : IEntityTypeConfiguration<AccountsReceivablePayment>
{
    public void Configure(EntityTypeBuilder<AccountsReceivablePayment> builder)
    {
        builder.ToTable("AccountsReceivablePayments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 4);

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(250);

        // Relaciones
        builder.HasOne(x => x.AccountsReceivable)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.AccountsReceivableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CashSession)
            .WithMany()
            .HasForeignKey(x => x.CashSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restricciones Check de integridad
        builder.ToTable(tb => tb
            .HasCheckConstraint("CK_ARPayments_Amount_Positive", "[Amount] > 0"));

        // Filtro de borrado lógico (Soft Delete)
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
