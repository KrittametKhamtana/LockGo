using LockGo.Domain.Entities;

namespace LockGo.Application.Interfaces;

public interface IReservationRepository
{
    /// <summary>Includes Compartment + Locker — a hit here (idempotent replay) is mapped straight to a ReservationDto.</summary>
    Task<Reservation?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct);

    /// <summary>Includes Compartment + Locker so callers can build a ReservationDto without extra round-trips.</summary>
    Task<Reservation?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Authoritative overlap check against live rows — the source of truth for the
    /// booking write path, never the denormalized Compartment.Status field.
    /// </summary>
    Task<bool> HasOverlapAsync(Guid compartmentId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);

    void Add(Reservation reservation);
}
