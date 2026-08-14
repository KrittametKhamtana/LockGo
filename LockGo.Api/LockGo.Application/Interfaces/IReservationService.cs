using LockGo.Application.DTOs;

namespace LockGo.Application.Interfaces;

public interface IReservationService
{
    /// <summary>
    /// Idempotent: replaying the same IdempotencyKey (e.g. a double-clicked
    /// Confirm button) returns the original reservation instead of creating a
    /// duplicate. Throws ConflictException (409) if the compartment isn't
    /// available for the requested window, NotFoundException (404) if the
    /// compartment doesn't exist.
    /// </summary>
    Task<ReservationDto> CreateAsync(CreateReservationRequest request, CancellationToken ct);

    /// <summary>Throws NotFoundException if the reservation doesn't exist.</summary>
    Task<ReservationDto> GetByIdAsync(Guid id, CancellationToken ct);
}
