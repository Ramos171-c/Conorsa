using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class PurchaseReceiptDetailConfiguration : IEntityTypeConfiguration<PurchaseReceiptDetail>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiptDetail> builder)
    {
        builder.ToTable("PurchaseReceiptDetails");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Quantity)
            .HasPrecision(18, 4);

        builder.Property(d => d.UnitPrice)
            .HasPrecision(18, 4);

        // Restricción: cantidad recibida > 0
        builder.ToTable(t => t.HasCheckConstraint("CK_PurchaseReceiptDetail_Quantity", "[Quantity] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_PurchaseReceiptDetail_UnitPrice", "[UnitPrice] >= 0"));

        // Relaciones
        builder.HasOne(d => d.PurchaseReceipt)
            .WithMany(r => r.Details)
            .HasForeignKey(d => d.PurchaseReceiptId)
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
