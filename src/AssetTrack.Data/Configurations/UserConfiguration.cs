using AssetTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTrack.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Company).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Division).HasMaxLength(100).IsRequired();
        builder.Property(u => u.City).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Designation).HasMaxLength(100);
        builder.Property(u => u.Contact).HasMaxLength(30);

        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Company);
        builder.HasIndex(u => u.Division);
        builder.HasIndex(u => u.City);
    }
}
