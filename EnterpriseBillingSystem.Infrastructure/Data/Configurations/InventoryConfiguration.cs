using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventory");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.PhysicalStock)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        builder.Property(i => i.ReservedStock)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        builder.Property(i => i.CommittedStock)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000);

        // SQL check constraints to prevent negative stock and check available stock >= 0
        builder.ToTable(i => i.HasCheckConstraint("CK_Inventory_ReservedStock", "[ReservedStock] >= 0.0000"));
        builder.ToTable(i => i.HasCheckConstraint("CK_Inventory_CommittedStock", "[CommittedStock] >= 0.0000"));

        // Relaciones
        builder.HasOne(i => i.BranchWarehouse)
            .WithMany(bw => bw.Inventories)
            .HasForeignKey(i => i.BranchWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Concurrencia Optimista (RowVersion)
        builder.Property(i => i.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Índice Único por Bodega y Producto
        builder.HasIndex(i => new { i.BranchWarehouseId, i.ProductId })
            .IsUnique();
    }
}
