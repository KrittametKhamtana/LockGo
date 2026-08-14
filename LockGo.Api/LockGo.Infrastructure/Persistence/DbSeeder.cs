using LockGo.Application.Common;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Persistence;

/// <summary>
/// Seeds the mock single user plus a handful of lockers/compartments so the
/// Find/Detail/Reservation screens have something to show without a real
/// admin/back-office flow. Idempotent — safe to call on every startup.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(LockGoDbContext db, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(ct))
        {
            db.Users.Add(new User { Id = MockUser.Id, Name = MockUser.Name });
        }

        if (!await db.Lockers.AnyAsync(ct))
        {
            var lockers = new[]
            {
                new Locker
                {
                    Id = Guid.NewGuid(),
                    Name = "LockGo Central Station",
                    Address = "1 Silom Road, Bangkok",
                    Lat = 13.7278,
                    Lng = 100.5241,
                    OperatingStatus = OperatingStatus.Open,
                },
                new Locker
                {
                    Id = Guid.NewGuid(),
                    Name = "LockGo Riverside Mall",
                    Address = "88 Charoen Nakhon Road, Bangkok",
                    Lat = 13.7223,
                    Lng = 100.5099,
                    OperatingStatus = OperatingStatus.Open,
                },
                new Locker
                {
                    Id = Guid.NewGuid(),
                    Name = "LockGo Airport Hub",
                    Address = "999 Suvarnabhumi Airport, Bangkok",
                    Lat = 13.6900,
                    Lng = 100.7501,
                    OperatingStatus = OperatingStatus.Closed,
                },
            };

            foreach (var locker in lockers)
            {
                locker.Compartments = new List<Compartment>
                {
                    new() { Id = Guid.NewGuid(), LockerId = locker.Id, Size = CompartmentSize.S, Price = 20m, Status = CompartmentStatus.Available },
                    new() { Id = Guid.NewGuid(), LockerId = locker.Id, Size = CompartmentSize.M, Price = 35m, Status = CompartmentStatus.Available },
                    new() { Id = Guid.NewGuid(), LockerId = locker.Id, Size = CompartmentSize.L, Price = 50m, Status = CompartmentStatus.Available },
                };
            }

            db.Lockers.AddRange(lockers);
        }

        await db.SaveChangesAsync(ct);
    }
}
