using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");

        builder.HasKey(ca => ca.Id);

        builder.Property(ca => ca.AddressLine1)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ca => ca.AddressLine2)
            .HasMaxLength(200);

        builder.Property(ca => ca.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ca => ca.State)
            .HasMaxLength(100);

        builder.Property(ca => ca.ZipCode)
            .HasMaxLength(20);

        builder.Property(ca => ca.Country)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("Nicaragua");

        builder.Property(ca => ca.AddressType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Principal");

        builder.HasOne(ca => ca.Customer)
            .WithMany(c => c.Addresses)
            .HasForeignKey(ca => ca.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft Delete filter
        builder.HasQueryFilter(ca => !ca.IsDeleted);
    }
}
