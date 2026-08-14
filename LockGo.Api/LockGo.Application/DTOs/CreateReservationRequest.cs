namespace LockGo.Application.DTOs;

/// <summary>
/// StartTime is always set server-side to the moment the reservation is
/// confirmed (immediate-use locker booking) — the client only picks how long
/// to hold the compartment for, matching the Reservation screen's Duration field.
/// </summary>
public record CreateReservationRequest(
    Guid CompartmentId,
    int DurationHours,
    string IdempotencyKey);
