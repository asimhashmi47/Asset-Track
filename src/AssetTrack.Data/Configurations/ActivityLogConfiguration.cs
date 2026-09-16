using AssetTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetTrack.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.Property(a => a.Description).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.HasIndex(a => a.TimestampUtc);
        builder.HasIndex(a => a.AssetId);
        builder.HasIndex(a => a.TargetUserId);

        builder.HasOne(a => a.Asset)
            .WithMany(a => a.History)
            .HasForeignKey(a => a.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ActorUser)
            .WithMany()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.TargetUser)
            .WithMany()
            .HasForeignKey(a => a.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
