using AssetTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTrack.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(a => a.AssetTag).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(50).IsRequired();
        builder.Property(a => a.SerialNumber).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Company).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Division).HasMaxLength(100);
        builder.Property(a => a.City).HasMaxLength(100);
        builder.Property(a => a.Make).HasMaxLength(100);
        builder.Property(a => a.Model).HasMaxLength(100);

        builder.HasIndex(a => a.AssetTag).IsUnique();
        builder.HasIndex(a => a.SerialNumber).IsUnique();
        builder.HasIndex(a => a.Name);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.Category);
        builder.HasIndex(a => a.CurrentUserId);

        builder.HasOne(a => a.CurrentUser)
            .WithMany(u => u.AssignedAssets)
            .HasForeignKey(a => a.CurrentUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
