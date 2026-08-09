using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class RouteLiquidationDetailConfiguration : IEntityTypeConfiguration<RouteLiquidationDetail>
{
    public void Configure(EntityTypeBuilder<RouteLiquidationDetail> builder)
    {
        builder.ToTable("RouteLiquidationDetails");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.QuantitySent)
            .HasPrecision(18, 4);

        builder.Property(d => d.QuantityReturned)
            .HasPrecision(18, 4);

        builder.Property(d => d.QuantitySold)
            .HasPrecision(18, 4);

        builder.Property(d => d.BaseQuantitySent)
            .HasPrecision(18, 4);

        builder.Property(d => d.BaseQuantityReturned)
            .HasPrecision(18, 4);

        builder.Property(d => d.BaseQuantitySold)
            .HasPrecision(18, 4);

        builder.Property(d => d.SalePrice)
            .HasPrecision(18, 4);

        builder.Property(d => d.Cost)
            .HasPrecision(18, 4);

        builder.Property(d => d.SubtotalSold)
            .HasPrecision(18, 4);

        builder.Property(d => d.SubtotalReturned)
            .HasPrecision(18, 4);

        builder.Property(d => d.Notes)
            .HasMaxLength(250);

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
