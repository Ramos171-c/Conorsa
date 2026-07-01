using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class CustomerPhoneConfiguration : IEntityTypeConfiguration<CustomerPhone>
{
    public void Configure(EntityTypeBuilder<CustomerPhone> builder)
    {
        builder.ToTable("CustomerPhones");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.PhoneNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(cp => cp.PhoneType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Celular");

        builder.HasOne(cp => cp.Customer)
            .WithMany(c => c.Phones)
            .HasForeignKey(cp => cp.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft Delete filter
        builder.HasQueryFilter(cp => !cp.IsDeleted);
    }
}
