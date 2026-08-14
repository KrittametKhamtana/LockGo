using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
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

    public void Add(Reservation reservation)
    {
        _db.Reservations.Add(reservation);
    }
}
