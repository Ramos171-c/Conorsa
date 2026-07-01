using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class SystemParameterConfiguration : IEntityTypeConfiguration<SystemParameter>
{
    public void Configure(EntityTypeBuilder<SystemParameter> builder)
    {
        builder.ToTable("SystemParameters");
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Key).IsRequired().HasMaxLength(100);
        builder.Property(sp => sp.Value).IsRequired().HasMaxLength(500);
        builder.Property(sp => sp.Description).HasMaxLength(500);
        
        builder.HasIndex(sp => sp.Key).IsUnique();
    }
}
