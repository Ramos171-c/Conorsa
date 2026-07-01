using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferenceDocument)
            .HasMaxLength(50);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 4);

        builder.Property(x => x.Notes)
            .HasMaxLength(250);

        builder.Property(x => x.Reason)
            .HasMaxLength(250);

        // Relaciones
        builder.HasOne(x => x.CashSession)
            .WithMany(x => x.CashMovements)
            .HasForeignKey(x => x.CashSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Check constraint to ensure Amount is positive
        builder.ToTable(tb => tb.HasCheckConstraint("CK_CashMovements_Amount_Positive", "[Amount] >= 0"));
    }
}
