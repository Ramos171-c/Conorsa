using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class CustomerCategoryConfiguration : IEntityTypeConfiguration<CustomerCategory>
{
    public void Configure(EntityTypeBuilder<CustomerCategory> builder)
    {
        builder.ToTable("CustomerCategories", t =>
        {
            t.HasCheckConstraint("CK_CustomerCategories_Discount", "[DefaultDiscountPercentage] >= 0.00 AND [DefaultDiscountPercentage] <= 100.00");
        });

        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cc => cc.Description)
            .HasMaxLength(250);

        builder.Property(cc => cc.DefaultDiscountPercentage)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0.00m);

        // Indexes
        builder.HasIndex(cc => cc.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Soft Delete filter
        builder.HasQueryFilter(cc => !cc.IsDeleted);
    }
}
