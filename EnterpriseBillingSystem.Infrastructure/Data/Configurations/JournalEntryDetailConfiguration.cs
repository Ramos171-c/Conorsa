using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class JournalEntryDetailConfiguration : IEntityTypeConfiguration<JournalEntryDetail>
{
    public void Configure(EntityTypeBuilder<JournalEntryDetail> builder)
    {
        builder.ToTable("JournalEntryDetails");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DebitAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.CreditAmount)
            .HasPrecision(18, 4);

        builder.Property(x => x.Description)
            .HasMaxLength(250);

        // Relaciones
        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restricciones Check para partida doble y coherencia de importes
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_JournalEntryDetails_Amounts", "([DebitAmount] >= 0 AND [CreditAmount] >= 0) AND (([DebitAmount] > 0 AND [CreditAmount] = 0) OR ([DebitAmount] = 0 AND [CreditAmount] > 0))");
        });

        // Filtro de borrado lógico
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
