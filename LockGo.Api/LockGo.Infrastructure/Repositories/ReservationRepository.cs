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

    public async Task<Reservation?> GetByBookingNumberAsync(string bookingNumber, CancellationToken ct)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Include(r => r.Compartment).ThenInclude(c => c.Locker)
            .FirstOrDefaultAsync(r => r.BookingNumber == bookingNumber, ct);
    }

    public void Add(Reservation reservation)
    {
        _db.Reservations.Add(reservation);
    }
}
