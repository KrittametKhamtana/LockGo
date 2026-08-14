using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;
using LockGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly LockGoDbContext _db;

    public ReservationRepository(LockGoDbContext db)
    {
        _db = db;
    }

    public async Task<Reservation?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct)
    {
        return await _db.Reservations
            .Include(r => r.Compartment).ThenInclude(c => c.Locker)
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task<Reservation?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Include(r => r.Compartment).ThenInclude(c => c.Locker)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<bool> HasOverlapAsync(Guid compartmentId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        // Status == Active alone isn't enough — a reservation whose EndTime has
        // already passed is still stored as Active (no expiry cron job), but
        // start is always "now" for new bookings, so EndTime > start already
        // excludes anything that's effectively expired.
        return await _db.Reservations.AnyAsync(
            r => r.CompartmentId == compartmentId &&
                 r.Status == ReservationStatus.Active &&
                 r.StartTime < end &&
                 r.EndTime > start,
            ct);
    }

    public void Add(Reservation reservation)
    {
        _db.Reservations.Add(reservation);
    }
}
