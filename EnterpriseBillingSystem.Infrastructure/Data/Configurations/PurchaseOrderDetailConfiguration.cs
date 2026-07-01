using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class PurchaseOrderDetailConfiguration : IEntityTypeConfiguration<PurchaseOrderDetail>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderDetail> builder)
    {
        builder.ToTable("PurchaseOrderDetails");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Quantity)
            .HasPrecision(18, 4);

        builder.Property(d => d.ReceivedQuantity)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000m);

        builder.Property(d => d.UnitPrice)
            .HasPrecision(18, 4);

        builder.Property(d => d.DiscountPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(0.00m);

        builder.Property(d => d.DiscountAmount)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000m);

        builder.Property(d => d.TaxPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(0.00m);

        builder.Property(d => d.TaxAmount)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000m);

        builder.Property(d => d.NetAmount)
            .HasPrecision(18, 4);

        // Restricciones: cantidad > 0
        builder.ToTable(t => t.HasCheckConstraint("CK_PurchaseOrderDetail_Quantity", "[Quantity] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_PurchaseOrderDetail_ReceivedQty", "[ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [Quantity]"));

        // Relaciones
        builder.HasOne(d => d.PurchaseOrder)
            .WithMany(po => po.Details)
            .HasForeignKey(d => d.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(d => d.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
