using LockGo.Domain.Entities;
using LockGo.Domain.Enums;

namespace LockGo.Application.Interfaces;

public interface ICompartmentRepository
{
    /// <summary>
    /// Returns one compartment of the requested size in the given locker that
    /// has no overlapping active reservation, or null if every one is taken.
    /// The free/taken decision is made against live Reservation rows (not the
    /// denormalized Status column) and must run inside the booking
    /// transaction — see ReservationService.
    /// Includes the parent Locker so callers can build a ReservationDto
    /// without a second round-trip.
    /// </summary>
    Task<Compartment?> FindAvailableAsync(
        int lockerId,
        CompartmentSize size,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct);

    /// <summary>
    /// Whether this locker offers the size at all, regardless of availability —
    /// lets the booking path answer "fully booked" (409) separately from
    /// "this locker doesn't have that size" (404).
    /// </summary>
    Task<bool> ExistsForSizeAsync(int lockerId, CompartmentSize size, CancellationToken ct);
}
