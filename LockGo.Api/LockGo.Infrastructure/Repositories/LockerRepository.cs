using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;
using LockGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Repositories;

public class LockerRepository : ILockerRepository
{
    private readonly LockGoDbContext _db;

    public LockerRepository(LockGoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Locker>> SearchAsync(LockerSearchQuery query, CancellationToken ct)
    {
        var lockers = _db.Lockers
            .AsNoTracking()
            .Include(l => l.Compartments)
            .AsQueryable();

        var hasSizeFilter = Enum.TryParse<CompartmentSize>(query.Size, ignoreCase: true, out var size);
        var availableOnly = query.AvailableOnly == true;

        // Size and availability must be checked on the SAME compartment — two
        // separate Any() calls would pass a locker whose only available
        // compartment is the wrong size (its occupied S plus available M both
        // satisfy their own Any() independently, even with no available S).
        if (hasSizeFilter && availableOnly)
        {
            lockers = lockers.Where(l => l.Compartments.Any(c => c.Size == size && c.Status == CompartmentStatus.Available));
        }
        else if (hasSizeFilter)
        {
            lockers = lockers.Where(l => l.Compartments.Any(c => c.Size == size));
        }
        else if (availableOnly)
        {
            lockers = lockers.Where(l => l.Compartments.Any(c => c.Status == CompartmentStatus.Available));
        }

        // Distance filtering can't be pushed down as SQL (Haversine over two runtime
        // coordinates), so it's applied in the service layer after this query returns.
        return await lockers.ToListAsync(ct);
    }

    public async Task<Locker?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Lockers
            .AsNoTracking()
            .Include(l => l.Compartments)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }
}
