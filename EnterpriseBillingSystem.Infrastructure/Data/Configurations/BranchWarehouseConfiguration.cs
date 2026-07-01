using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class BranchWarehouseConfiguration : IEntityTypeConfiguration<BranchWarehouse>
{
    public void Configure(EntityTypeBuilder<BranchWarehouse> builder)
    {
        builder.ToTable("BranchWarehouses");

        builder.HasKey(bw => bw.Id);

        builder.HasOne(bw => bw.Branch)
            .WithMany()
            .HasForeignKey(bw => bw.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bw => bw.Warehouse)
            .WithMany(w => w.BranchWarehouses)
            .HasForeignKey(bw => bw.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(bw => bw.AllowNegativeInventory)
            .HasDefaultValue(false);

        // Índices
        builder.HasIndex(bw => bw.BranchId);

        // Soft Delete filter
        builder.HasQueryFilter(bw => !bw.IsDeleted);
    }
}
