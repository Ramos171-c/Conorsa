using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class PurchaseInvoiceDetailConfiguration : IEntityTypeConfiguration<PurchaseInvoiceDetail>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceDetail> builder)
    {
        builder.ToTable("PurchaseInvoiceDetails");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Quantity)
            .HasPrecision(18, 4);

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

        // Restricciones
        builder.ToTable(t => t.HasCheckConstraint("CK_PurchaseInvoiceDetail_Quantity", "[Quantity] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_PurchaseInvoiceDetail_UnitPrice", "[UnitPrice] >= 0"));

        // Relaciones
        builder.HasOne(d => d.PurchaseInvoice)
            .WithMany(pi => pi.Details)
            .HasForeignKey(d => d.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(d => d.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ProductPresentation)
            .WithMany()
            .HasForeignKey(d => d.ProductPresentationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
