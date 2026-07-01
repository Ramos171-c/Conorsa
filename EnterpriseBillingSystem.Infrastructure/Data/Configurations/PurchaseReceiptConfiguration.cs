using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class PurchaseReceiptConfiguration : IEntityTypeConfiguration<PurchaseReceipt>
{
    public void Configure(EntityTypeBuilder<PurchaseReceipt> builder)
    {
        builder.ToTable("PurchaseReceipts");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReceiptNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.ReferenceDocument)
            .HasMaxLength(100);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        // Concurrencia Optimista
        builder.Property(r => r.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Índice único sobre número de recepción
        builder.HasIndex(r => r.ReceiptNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relaciones
        builder.HasOne(r => r.Supplier)
            .WithMany(s => s.PurchaseReceipts)
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.PurchaseOrder)
            .WithMany(po => po.PurchaseReceipts)
            .HasForeignKey(r => r.PurchaseOrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.BranchWarehouse)
            .WithMany()
            .HasForeignKey(r => r.BranchWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Details)
            .WithOne(d => d.PurchaseReceipt)
            .HasForeignKey(d => d.PurchaseReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
