using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class CustomerEmailConfiguration : IEntityTypeConfiguration<CustomerEmail>
{
    public void Configure(EntityTypeBuilder<CustomerEmail> builder)
    {
        builder.ToTable("CustomerEmails");

        builder.HasKey(ce => ce.Id);

        builder.Property(ce => ce.EmailAddress)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(ce => ce.EmailType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Personal");

        builder.HasOne(ce => ce.Customer)
            .WithMany(c => c.Emails)
            .HasForeignKey(ce => ce.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft Delete filter
        builder.HasQueryFilter(ce => !ce.IsDeleted);
    }
}
