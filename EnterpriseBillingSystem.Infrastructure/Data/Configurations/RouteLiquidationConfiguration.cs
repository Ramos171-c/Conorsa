using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class RouteLiquidationConfiguration : IEntityTypeConfiguration<RouteLiquidation>
{
    public void Configure(EntityTypeBuilder<RouteLiquidation> builder)
    {
        builder.ToTable("RouteLiquidations");

        builder.HasKey(rl => rl.Id);

        builder.Property(rl => rl.LiquidationNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(rl => rl.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(rl => rl.TotalQuantitySent)
            .HasPrecision(18, 4);

        builder.Property(rl => rl.TotalQuantityReturned)
            .HasPrecision(18, 4);

        builder.Property(rl => rl.TotalQuantitySold)
            .HasPrecision(18, 4);

        builder.Property(rl => rl.TotalAmountSold)
            .HasPrecision(18, 4);

        builder.Property(rl => rl.TotalAmountReturned)
            .HasPrecision(18, 4);

        builder.Property(rl => rl.TotalCostSold)
            .HasPrecision(18, 4);

        builder.Property(rl => rl.EstimatedProfit)
            .HasPrecision(18, 4);

        builder.Property(rl => rl.Observations)
            .HasMaxLength(500);

        builder.HasIndex(rl => rl.LiquidationNumber)
            .IsUnique();

        builder.HasOne(rl => rl.Route)
            .WithMany()
            .HasForeignKey(rl => rl.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(rl => rl.Details)
            .WithOne(d => d.RouteLiquidation)
            .HasForeignKey(d => d.RouteLiquidationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
