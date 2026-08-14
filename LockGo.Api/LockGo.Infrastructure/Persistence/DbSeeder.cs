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
    private static readonly IReadOnlyDictionary<CompartmentSize, decimal> PriceBySize = new Dictionary<CompartmentSize, decimal>
    {
        [CompartmentSize.S] = 20m,
        [CompartmentSize.M] = 35m,
        [CompartmentSize.L] = 50m,
    };

    public static async Task SeedAsync(LockGoDbContext db, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(ct))
        {
            db.Users.Add(new User { Id = MockUser.Id, Name = MockUser.Name });
        }

        if (!await db.Lockers.AnyAsync(ct))
        {
            // Deliberately uneven inventory: Riverside has no Large and Airport
            // has no Small, so the UI has real cases where a location simply
            // doesn't offer a size (as opposed to offering it but being full).
            var lockers = new[]
            {
                CreateLocker(
                    "LockGo Central Station", "1 Silom Road, Bangkok", 13.7278, 100.5241, OperatingStatus.Open,
                    (CompartmentSize.S, 4), (CompartmentSize.M, 3), (CompartmentSize.L, 2)),

                CreateLocker(
                    "LockGo Riverside Mall", "88 Charoen Nakhon Road, Bangkok", 13.7223, 100.5099, OperatingStatus.Open,
                    (CompartmentSize.S, 3), (CompartmentSize.M, 2)),

                CreateLocker(
                    "LockGo Airport Hub", "999 Suvarnabhumi Airport, Bangkok", 13.6900, 100.7501, OperatingStatus.Closed,
                    (CompartmentSize.M, 4), (CompartmentSize.L, 3)),

                CreateLocker(
                    "LockGo Siam Square", "22 Rama I Road, Bangkok", 13.7455, 100.5340, OperatingStatus.Open,
                    (CompartmentSize.S, 2), (CompartmentSize.M, 2), (CompartmentSize.L, 1)),

                CreateLocker(
                    "LockGo Chatuchak Market", "587 Kamphaeng Phet 2 Road, Bangkok", 13.7999, 100.5503, OperatingStatus.Open,
                    (CompartmentSize.S, 5), (CompartmentSize.L, 2)),
            };

            db.Lockers.AddRange(lockers);
        }

        await db.SaveChangesAsync(ct);
    }

    private static Locker CreateLocker(
        string name,
        string address,
        double lat,
        double lng,
        OperatingStatus operatingStatus,
        params (CompartmentSize Size, int Count)[] inventory)
    {
        var locker = new Locker
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            Lat = lat,
            Lng = lng,
            OperatingStatus = operatingStatus,
        };

        locker.Compartments = inventory
            .SelectMany(entry => Enumerable.Range(0, entry.Count).Select(_ => new Compartment
            {
                Id = Guid.NewGuid(),
                LockerId = locker.Id,
                Size = entry.Size,
                Price = PriceBySize[entry.Size],
                Status = CompartmentStatus.Available,
            }))
            .ToList();

        return locker;
    }
}
