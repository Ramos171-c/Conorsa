using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("AccountingPeriods");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClosedBy)
            .HasMaxLength(450);

        // Índice compuesto único en Year y Month
        builder.HasIndex(x => new { x.Year, x.Month })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Filtro de borrado lógico
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
