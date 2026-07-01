using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("SalesInvoices");

        builder.HasKey(si => si.Id);

        builder.Property(si => si.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(25);

        // Snapshots históricos
        builder.Property(si => si.CustomerNameSnapshot)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(si => si.CustomerIdentificationSnapshot)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(si => si.CustomerType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(CustomerType.Natural);

        builder.Property(si => si.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(si => si.SubTotal)
            .HasPrecision(18, 4);

        builder.Property(si => si.DiscountAmount)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000m);

        builder.Property(si => si.TaxAmount)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000m);

        builder.Property(si => si.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(si => si.PaymentTermsDays)
            .HasDefaultValue(0);

        builder.Property(si => si.Notes)
            .HasMaxLength(500);

        builder.Property(si => si.CancellationReason)
            .HasMaxLength(500);

        // Concurrencia optimista
        builder.Property(si => si.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Índice único por número de factura
        builder.HasIndex(si => si.InvoiceNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relaciones
        builder.HasOne(si => si.Customer)
            .WithMany()
            .HasForeignKey(si => si.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.BranchWarehouse)
            .WithMany()
            .HasForeignKey(si => si.BranchWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(si => si.SalesOrder)
            .WithMany(so => so.SalesInvoices)
            .HasForeignKey(si => si.SalesOrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Auto-referencia para notas de crédito futuras
        builder.HasOne(si => si.OriginalInvoice)
            .WithMany()
            .HasForeignKey(si => si.OriginalInvoiceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(si => si.Details)
            .WithOne(d => d.SalesInvoice)
            .HasForeignKey(d => d.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(si => !si.IsDeleted);
    }
}
