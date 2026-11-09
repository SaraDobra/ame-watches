using AmeWatches.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmeWatches.Api.Data.EntityConfig;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.BrandId, x.Name }).IsUnique();

        b.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Products).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
