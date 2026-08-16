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

    /// <summary>
    /// Fetches by public BookingNumber rather than the sequential id, so a
    /// confirmation permalink can't be walked to read other people's bookings.
    /// Throws NotFoundException if the reservation doesn't exist.
    /// </summary>
    Task<ReservationDto> GetByBookingNumberAsync(string bookingNumber, CancellationToken ct);
}
