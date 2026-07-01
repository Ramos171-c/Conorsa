using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class SalesInvoiceDetailConfiguration : IEntityTypeConfiguration<SalesInvoiceDetail>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceDetail> builder)
    {
        builder.ToTable("SalesInvoiceDetails");

        builder.HasKey(d => d.Id);

        // Snapshots históricos
        builder.Property(d => d.ProductCodeSnapshot)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.ProductNameSnapshot)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.UnitOfMeasureSnapshot)
            .IsRequired()
            .HasMaxLength(20);

        // Cantidades y precios
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

        // Constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_SalesInvoiceDetail_Quantity", "[Quantity] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_SalesInvoiceDetail_UnitPrice", "[UnitPrice] >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_SalesInvoiceDetail_DiscountPct", "[DiscountPercentage] >= 0 AND [DiscountPercentage] <= 100"));

        // Relaciones
        builder.HasOne(d => d.SalesInvoice)
            .WithMany(si => si.Details)
            .HasForeignKey(d => d.SalesInvoiceId)
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
