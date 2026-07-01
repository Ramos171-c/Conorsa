using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class InventoryMovementDetailConfiguration : IEntityTypeConfiguration<InventoryMovementDetail>
{
    public void Configure(EntityTypeBuilder<InventoryMovementDetail> builder)
    {
        builder.ToTable("InventoryMovementDetails");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(d => d.ConversionFactor)
            .HasPrecision(18, 6)
            .HasDefaultValue(1.000000);

        builder.Property(d => d.QuantityInBaseUnit)
            .HasPrecision(18, 4)
            .IsRequired();

        // SQL check constraints
        builder.ToTable(d => d.HasCheckConstraint("CK_InventoryMovementDetails_Quantity", "[Quantity] > 0.0000"));
        builder.ToTable(d => d.HasCheckConstraint("CK_InventoryMovementDetails_ConversionFactor", "[ConversionFactor] > 0.000000"));
        builder.ToTable(d => d.HasCheckConstraint("CK_InventoryMovementDetails_QuantityInBaseUnit", "[QuantityInBaseUnit] > 0.0000"));

        // Relaciones
        builder.HasOne(d => d.InventoryMovement)
            .WithMany(m => m.Details)
            .HasForeignKey(d => d.InventoryMovementId)
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

        // Índices
        builder.HasIndex(d => d.ProductId);
    }
}
