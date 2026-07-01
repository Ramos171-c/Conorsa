using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class AccountsPayablePaymentConfiguration : IEntityTypeConfiguration<AccountsPayablePayment>
{
    public void Configure(EntityTypeBuilder<AccountsPayablePayment> builder)
    {
        builder.ToTable("AccountsPayablePayments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 4);

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(250);

        // Relaciones
        builder.HasOne(x => x.AccountsPayable)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.AccountsPayableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CashSession)
            .WithMany()
            .HasForeignKey(x => x.CashSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Filtro de borrado lógico (Soft Delete)
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
