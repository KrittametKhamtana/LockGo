using LockGo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Persistence;

public class LockGoDbContext : DbContext
{
    public LockGoDbContext(DbContextOptions<LockGoDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Locker> Lockers => Set<Locker>();
    public DbSet<Compartment> Compartments => Set<Compartment>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LockGoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
