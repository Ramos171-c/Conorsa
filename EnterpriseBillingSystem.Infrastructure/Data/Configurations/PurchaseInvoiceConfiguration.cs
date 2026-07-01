using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("PurchaseInvoices");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pi => pi.InternalInvoiceNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pi => pi.SubTotal)
            .HasPrecision(18, 4);

        builder.Property(pi => pi.DiscountAmount)
            .HasPrecision(18, 4);

        builder.Property(pi => pi.TaxAmount)
            .HasPrecision(18, 4);

        builder.Property(pi => pi.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(pi => pi.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(pi => pi.Notes)
            .HasMaxLength(500);

        builder.Property(pi => pi.PaymentTermsDays)
            .HasDefaultValue(0);

        // Índice único sobre número interno de factura
        builder.HasIndex(pi => pi.InternalInvoiceNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relaciones
        builder.HasOne(pi => pi.Supplier)
            .WithMany(s => s.PurchaseInvoices)
            .HasForeignKey(pi => pi.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.PurchaseReceipt)
            .WithMany()
            .HasForeignKey(pi => pi.PurchaseReceiptId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pi => pi.PurchaseOrder)
            .WithMany()
            .HasForeignKey(pi => pi.PurchaseOrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pi => pi.Details)
            .WithOne(d => d.PurchaseInvoice)
            .HasForeignKey(d => d.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(pi => !pi.IsDeleted);
    }
}
