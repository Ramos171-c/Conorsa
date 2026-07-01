using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class PricingThresholdConfiguration : IEntityTypeConfiguration<PricingThreshold>
{
    public void Configure(EntityTypeBuilder<PricingThreshold> builder)
    {
        builder.ToTable("PricingThresholds");

        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.LevelName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pt => pt.MinimumSubtotal)
            .HasPrecision(18, 2);

        builder.HasIndex(pt => pt.LevelName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(pt => !pt.IsDeleted);
    }
}
