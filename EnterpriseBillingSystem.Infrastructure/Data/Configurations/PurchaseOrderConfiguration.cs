using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(po => po.Id);

        builder.Property(po => po.OrderNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(po => po.SubTotal)
            .HasPrecision(18, 4);

        builder.Property(po => po.DiscountAmount)
            .HasPrecision(18, 4);

        builder.Property(po => po.TaxAmount)
            .HasPrecision(18, 4);

        builder.Property(po => po.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(po => po.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(po => po.Notes)
            .HasMaxLength(500);

        // Concurrencia Optimista
        builder.Property(po => po.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Índice único sobre número de orden por sucursal
        builder.HasIndex(po => po.OrderNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relaciones
        builder.HasOne(po => po.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(po => po.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(po => po.Details)
            .WithOne(d => d.PurchaseOrder)
            .HasForeignKey(d => d.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(po => po.PurchaseReceipts)
            .WithOne(r => r.PurchaseOrder)
            .HasForeignKey(r => r.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(po => !po.IsDeleted);
    }
}
