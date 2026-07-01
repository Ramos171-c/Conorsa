using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntryNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.SourceModule)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PostedByUserId)
            .HasMaxLength(450);

        builder.Property(x => x.ReferenceDocument)
            .HasMaxLength(50);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Relación con detalles
        builder.HasMany(x => x.Details)
            .WithOne(x => x.JournalEntry)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice único en EntryNumber (filtrado por Soft Delete)
        builder.HasIndex(x => x.EntryNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Filtro de borrado lógico
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
