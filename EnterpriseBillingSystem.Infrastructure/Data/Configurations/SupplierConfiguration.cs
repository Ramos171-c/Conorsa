using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SupplierCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.IdentificationNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.IdentificationType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.LegalName)
            .HasMaxLength(200);

        builder.Property(s => s.Phone)
            .HasMaxLength(30);

        builder.Property(s => s.Email)
            .HasMaxLength(150);

        builder.Property(s => s.Address)
            .HasMaxLength(300);

        builder.Property(s => s.ContactName)
            .HasMaxLength(150);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Índices únicos (aplican solo sobre activos)
        builder.HasIndex(s => s.SupplierCode)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(s => s.IdentificationNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relaciones
        builder.HasOne(s => s.SupplierCategory)
            .WithMany(sc => sc.Suppliers)
            .HasForeignKey(s => s.SupplierCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
