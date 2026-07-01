using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class CustomerPricingProfileConfiguration : IEntityTypeConfiguration<CustomerPricingProfile>
{
    public void Configure(EntityTypeBuilder<CustomerPricingProfile> builder)
    {
        builder.ToTable("CustomerPricingProfiles");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // Soft Delete filter
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
