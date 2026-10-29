using AmeWatches.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmeWatches.Api.Data.EntityConfig;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
        b.Property(x => x.ModelCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");

        b.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
