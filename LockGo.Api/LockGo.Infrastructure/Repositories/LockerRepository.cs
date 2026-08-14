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

        if (Enum.TryParse<CompartmentSize>(query.Size, ignoreCase: true, out var size))
        {
            lockers = lockers.Where(l => l.Compartments.Any(c => c.Size == size));
        }

        if (query.AvailableOnly == true)
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
