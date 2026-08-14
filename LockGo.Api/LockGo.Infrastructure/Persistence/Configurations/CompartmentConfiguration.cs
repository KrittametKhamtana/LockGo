using LockGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LockGo.Infrastructure.Persistence.Configurations;

public class CompartmentConfiguration : IEntityTypeConfiguration<Compartment>
{
    public void Configure(EntityTypeBuilder<Compartment> builder)
    {
        builder.ToTable("compartments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Size).HasConversion<string>().HasMaxLength(1);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Price).HasPrecision(10, 2);

        // Free-tier Postgres: no extra RowVersion column — reuse the system xmin
        // column as the optimistic concurrency token (lighter than pessimistic locks).
        // UseXminAsConcurrencyToken() was removed upstream; the current provider
        // maps it as an ordinary shadow row-version property instead.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion();

        // Supports the list/filter endpoint (GET /api/lockers?size=&availability=)
        // without scanning Reservation rows.
        builder.HasIndex(c => new { c.LockerId, c.Size, c.Status });

        builder.HasMany(c => c.Reservations)
            .WithOne(r => r.Compartment)
            .HasForeignKey(r => r.CompartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
