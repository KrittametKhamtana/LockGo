using LockGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LockGo.Infrastructure.Persistence.Configurations;

public class LockerConfiguration : IEntityTypeConfiguration<Locker>
{
    public void Configure(EntityTypeBuilder<Locker> builder)
    {
        builder.ToTable("lockers");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Address).IsRequired().HasMaxLength(500);
        builder.Property(l => l.OperatingStatus).HasConversion<string>().HasMaxLength(20);

        // Search-by-location scans the whole table for this scope (distance is mocked),
        // so we only index what the list endpoint actually filters/sorts on.
        builder.HasIndex(l => l.OperatingStatus);

        builder.HasMany(l => l.Compartments)
            .WithOne(c => c.Locker)
            .HasForeignKey(c => c.LockerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
