using AssetTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTrack.Data.Configurations;

public class LookupItemConfiguration : IEntityTypeConfiguration<LookupItem>
{
    public void Configure(EntityTypeBuilder<LookupItem> builder)
    {
        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(l => new { l.Kind, l.Name }).IsUnique();
    }
}
