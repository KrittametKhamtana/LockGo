using LockGo.Domain.Entities;

namespace LockGo.Application.Interfaces;

public interface IReservationRepository
{
    /// <summary>Includes Compartment + Locker — a hit here (idempotent replay) is mapped straight to a ReservationDto.</summary>
    Task<Reservation?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct);

    /// <summary>
    /// Looks a reservation up by its public BookingNumber, not by the sequential
    /// primary key — the key is guessable, the booking number isn't. Includes
    /// Compartment + Locker so callers can build a ReservationDto without extra
    /// round-trips.
    /// </summary>
    Task<Reservation?> GetByBookingNumberAsync(string bookingNumber, CancellationToken ct);

    void Add(Reservation reservation);
}
