using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.ToTable("CustomerContacts");

        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cc => cc.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cc => cc.JobTitle)
            .HasMaxLength(100);

        builder.Property(cc => cc.Phone)
            .HasMaxLength(50);

        builder.Property(cc => cc.Email)
            .HasMaxLength(150);

        builder.Property(cc => cc.Notes)
            .HasMaxLength(250);

        builder.HasOne(cc => cc.Customer)
            .WithMany(c => c.Contacts)
            .HasForeignKey(cc => cc.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft Delete filter
        builder.HasQueryFilter(cc => !cc.IsDeleted);
    }
}
