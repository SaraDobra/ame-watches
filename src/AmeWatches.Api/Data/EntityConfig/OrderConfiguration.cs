using AmeWatches.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmeWatches.Api.Data.EntityConfig;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.Property(x => x.OrderNo).IsRequired().HasMaxLength(30);
        b.Property(x => x.AccessToken).IsRequired().HasMaxLength(64);
        b.Property(x => x.CustomerName).IsRequired().HasMaxLength(150);
        b.Property(x => x.Phone).IsRequired().HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Address).IsRequired().HasMaxLength(250);
        b.Property(x => x.City).IsRequired().HasMaxLength(100);

        b.Property(x => x.PaymentSessionId).HasMaxLength(100);

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);

        b.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.Shipping).HasColumnType("decimal(18,2)");
        b.Property(x => x.Total).HasColumnType("decimal(18,2)");

        b.HasIndex(x => x.OrderNo).IsUnique();
        b.HasIndex(x => x.PaymentSessionId).IsUnique().HasFilter("[PaymentSessionId] IS NOT NULL");
        b.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
