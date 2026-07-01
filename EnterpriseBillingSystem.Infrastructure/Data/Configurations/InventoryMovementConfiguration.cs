using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.MovementType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.ReferenceDocument)
            .HasMaxLength(100);

        builder.Property(m => m.Notes)
            .HasMaxLength(500);

        // SQL constraints
        builder.ToTable(m => m.HasCheckConstraint("CK_InventoryMovements_MovementType", "[MovementType] IN (1, 2, 3, 4, 5, 6)"));

        // Relaciones
        builder.HasOne(m => m.FromBranchWarehouse)
            .WithMany()
            .HasForeignKey(m => m.FromBranchWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ToBranchWarehouse)
            .WithMany()
            .HasForeignKey(m => m.ToBranchWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(m => m.MovementNumber)
            .IsUnique();
    }
}
