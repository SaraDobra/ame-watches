using AmeWatches.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmeWatches.Api.Data.EntityConfig;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(100);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}
