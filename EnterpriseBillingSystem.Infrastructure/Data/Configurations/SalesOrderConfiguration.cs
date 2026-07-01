using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseBillingSystem.Domain.Entities;

namespace EnterpriseBillingSystem.Infrastructure.Data.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");

        builder.HasKey(so => so.Id);

        builder.Property(so => so.OrderNumber)
            .IsRequired()
            .HasMaxLength(25);

        builder.Property(so => so.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(so => so.SubTotal)
            .HasPrecision(18, 4);

        builder.Property(so => so.DiscountAmount)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000m);

        builder.Property(so => so.TaxAmount)
            .HasPrecision(18, 4)
            .HasDefaultValue(0.0000m);

        builder.Property(so => so.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(so => so.Notes)
            .HasMaxLength(500);

        // Concurrencia optimista
        builder.Property(so => so.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Índice único por número de pedido
        builder.HasIndex(so => so.OrderNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Relaciones
        builder.HasOne(so => so.Customer)
            .WithMany()
            .HasForeignKey(so => so.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(so => so.Details)
            .WithOne(d => d.SalesOrder)
            .HasForeignKey(d => d.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(so => so.SalesInvoices)
            .WithOne(si => si.SalesOrder)
            .HasForeignKey(si => si.SalesOrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(so => !so.IsDeleted);
    }
}
