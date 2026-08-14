using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;
using LockGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Repositories;

public class CompartmentRepository : ICompartmentRepository
{
    private readonly LockGoDbContext _db;

    public CompartmentRepository(LockGoDbContext db)
    {
        _db = db;
    }

    public async Task<Compartment?> FindAvailableAsync(
        Guid lockerId,
        CompartmentSize size,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        // Tracked (no AsNoTracking): the booking path mutates Status on the
        // returned instance and relies on SaveChanges picking that up, plus the
        // xmin concurrency token to reject a racing writer.
        //
        // The !Any(...) is the authoritative availability test — live Reservation
        // rows, not the denormalized Status column. Ordering by Id keeps
        // assignment deterministic, which makes the concurrency tests reproducible.
        return await _db.Compartments
            .Include(c => c.Locker)
            .Where(c => c.LockerId == lockerId && c.Size == size)
            .Where(c => !c.Reservations.Any(r =>
                r.Status == ReservationStatus.Active &&
                r.StartTime < end &&
                r.EndTime > start))
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ExistsForSizeAsync(Guid lockerId, CompartmentSize size, CancellationToken ct)
    {
        return await _db.Compartments
            .AsNoTracking()
            .AnyAsync(c => c.LockerId == lockerId && c.Size == size, ct);
    }
}
