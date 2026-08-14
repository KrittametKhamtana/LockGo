using LockGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LockGo.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.BookingNumber).IsRequired().HasMaxLength(32);
        builder.Property(r => r.IdempotencyKey).IsRequired().HasMaxLength(64);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(r => r.BookingNumber).IsUnique();

        // Final safety net behind the in-transaction idempotency check (rule 4).
        builder.HasIndex(r => r.IdempotencyKey).IsUnique();

        // Supports the overlap check on the booking write path:
        // WHERE CompartmentId = X AND Status = Active AND StartTime < @end AND EndTime > @start
        builder.HasIndex(r => new { r.CompartmentId, r.Status, r.StartTime, r.EndTime });

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
