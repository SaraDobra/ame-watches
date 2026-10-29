using AmeWatches.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmeWatches.Api.Data.EntityConfig;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(200);
        b.Property(x => x.ModelCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Movement).HasMaxLength(50);
        b.Property(x => x.Price).HasColumnType("decimal(18,2)");
        b.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.ModelCode).IsUnique();
        b.HasIndex(x => new { x.IsActive, x.BrandId, x.Price });

        b.HasOne(x => x.Brand).WithMany(x => x.Products).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
    }
}
