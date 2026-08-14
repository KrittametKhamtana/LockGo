using LockGo.Application.Common;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Persistence;

/// <summary>
/// Seeds the mock single user plus a handful of lockers/compartments so the
/// Find/Detail/Reservation screens have something to show without a real
/// admin/back-office flow. Idempotent per-locker (matched by Name) rather
/// than an all-or-nothing gate — safe to call after new entries are added to
/// LockerDefinitions without wiping or duplicating what's already there.
/// </summary>
public static class DbSeeder
{
    private static readonly IReadOnlyDictionary<CompartmentSize, decimal> PriceBySize = new Dictionary<CompartmentSize, decimal>
    {
        [CompartmentSize.S] = 20m,
        [CompartmentSize.M] = 35m,
        [CompartmentSize.L] = 50m,
    };

    // Deliberately uneven inventory across sizes and lockers — some carry all
    // three sizes, some are missing one entirely (Don Mueang has only Small;
    // Riverside has no Large; Airport and Thonglor have no Small), so the UI
    // has real cases where a location simply doesn't offer a size at all
    // (as opposed to offering it but being full).
    private static readonly (string Name, string Address, double Lat, double Lng, OperatingStatus Status, (CompartmentSize Size, int Count)[] Inventory)[] LockerDefinitions =
    [
        ("LockGo Central Station", "1 Silom Road, Bangkok", 13.7278, 100.5241, OperatingStatus.Open,
            [(CompartmentSize.S, 4), (CompartmentSize.M, 3), (CompartmentSize.L, 2)]),

        ("LockGo Riverside Mall", "88 Charoen Nakhon Road, Bangkok", 13.7223, 100.5099, OperatingStatus.Open,
            [(CompartmentSize.S, 3), (CompartmentSize.M, 2)]),

        ("LockGo Airport Hub", "999 Suvarnabhumi Airport, Bangkok", 13.6900, 100.7501, OperatingStatus.Closed,
            [(CompartmentSize.M, 4), (CompartmentSize.L, 3)]),

        ("LockGo Siam Square", "22 Rama I Road, Bangkok", 13.7455, 100.5340, OperatingStatus.Open,
            [(CompartmentSize.S, 2), (CompartmentSize.M, 2), (CompartmentSize.L, 1)]),

        ("LockGo Chatuchak Market", "587 Kamphaeng Phet 2 Road, Bangkok", 13.7999, 100.5503, OperatingStatus.Open,
            [(CompartmentSize.S, 5), (CompartmentSize.L, 2)]),

        ("LockGo Asok Interchange", "159 Sukhumvit Road, Bangkok", 13.7373, 100.5602, OperatingStatus.Open,
            [(CompartmentSize.S, 3), (CompartmentSize.M, 1)]),

        ("LockGo Thonglor Community Mall", "55 Thonglor Soi 10, Bangkok", 13.7311, 100.5807, OperatingStatus.Open,
            [(CompartmentSize.M, 2), (CompartmentSize.L, 2)]),

        ("LockGo Don Mueang Airport", "222 Vibhavadi Rangsit Road, Bangkok", 13.9126, 100.6068, OperatingStatus.Open,
            [(CompartmentSize.S, 2)]),

        ("LockGo Silom Complex", "191 Silom Road, Bangkok", 13.7284, 100.5340, OperatingStatus.Closed,
            [(CompartmentSize.S, 1), (CompartmentSize.M, 1), (CompartmentSize.L, 1)]),

        ("LockGo On Nut BTS", "1000 Sukhumvit Road, Bangkok", 13.7055, 100.6014, OperatingStatus.Open,
            [(CompartmentSize.S, 4), (CompartmentSize.M, 3), (CompartmentSize.L, 2)]),
    ];

    public static async Task SeedAsync(LockGoDbContext db, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(ct))
        {
            db.Users.Add(new User { Id = MockUser.Id, Name = MockUser.Name });
        }

        var existingNames = await db.Lockers.Select(l => l.Name).ToListAsync(ct);

        var newLockers = LockerDefinitions
            .Where(def => !existingNames.Contains(def.Name))
            .Select(def => CreateLocker(def.Name, def.Address, def.Lat, def.Lng, def.Status, def.Inventory))
            .ToList();

        if (newLockers.Count > 0)
        {
            db.Lockers.AddRange(newLockers);
        }

        await db.SaveChangesAsync(ct);
    }

    private static Locker CreateLocker(
        string name,
        string address,
        double lat,
        double lng,
        OperatingStatus operatingStatus,
        (CompartmentSize Size, int Count)[] inventory)
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
