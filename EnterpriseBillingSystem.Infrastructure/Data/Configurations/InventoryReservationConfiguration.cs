using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(r => r.ReferenceDocument)
            .HasMaxLength(100);

        // SQL check constraints
        builder.ToTable(r => r.HasCheckConstraint("CK_InventoryReservations_Quantity", "[Quantity] > 0.0000"));

        // Relaciones
        builder.HasOne(r => r.BranchWarehouse)
            .WithMany()
            .HasForeignKey(r => r.BranchWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(r => new { r.ProductId, r.ExpiryDate })
            .HasFilter("[IsCompleted] = 0 AND [IsCancelled] = 0");
    }
}
