using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", t =>
        {
            t.HasCheckConstraint("CK_Customers_CreditLimit", "[CreditLimit] >= 0.0000");
            t.HasCheckConstraint("CK_Customers_CreditDays", "[CreditDays] >= 0");
            t.HasCheckConstraint("CK_Customers_Discount", "[DefaultDiscountPercentage] >= 0.00 AND [DefaultDiscountPercentage] <= 100.00");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.IdentificationNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.IdentificationType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.CustomerType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.LegalName)
            .HasMaxLength(150);

        builder.Property(c => c.CreditLimit)
            .HasColumnType("decimal(18,4)")
            .HasDefaultValue(0.0000m);

        builder.Property(c => c.CreditDays)
            .HasDefaultValue(0);

        builder.Property(c => c.CanUseCredit)
            .HasDefaultValue(false);

        builder.Property(c => c.IsTaxExempt)
            .HasDefaultValue(false);

        builder.Property(c => c.DefaultDiscountPercentage)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0.00m);

        builder.Property(c => c.Status)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.CustomerStatus.Active);

        // Relationships
        builder.HasOne(c => c.CustomerCategory)
            .WithMany(cc => cc.Customers)
            .HasForeignKey(c => c.CustomerCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CustomerPricingProfile)
            .WithMany()
            .HasForeignKey(c => c.CustomerPricingProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Route)
            .WithMany()
            .HasForeignKey(c => c.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique filtered indexes
        builder.HasIndex(c => c.CustomerCode)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => c.IdentificationNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Soft Delete filter
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
