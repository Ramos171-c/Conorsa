using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class SalesGoalConfiguration : IEntityTypeConfiguration<SalesGoal>
{
    public void Configure(EntityTypeBuilder<SalesGoal> builder)
    {
        builder.ToTable("SalesGoals");

        builder.HasKey(sg => sg.Id);

        builder.Property(sg => sg.PeriodName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sg => sg.TargetAmount)
            .HasPrecision(18, 2);

        builder.Property(sg => sg.CurrentAmount)
            .HasPrecision(18, 2);

        builder.HasOne(sg => sg.User)
            .WithMany(u => u.SalesGoals)
            .HasForeignKey(sg => sg.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(sg => !sg.IsDeleted);
    }
}
