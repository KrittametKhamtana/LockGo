namespace LockGo.Application.DTOs;

/// <summary>
/// The client books a locker + compartment size; the server assigns a
/// specific free compartment of that size inside the transaction. Picking the
/// compartment server-side is what makes "Small has 2 free" meaningful —
/// two users choosing "Small" concurrently get different compartments rather
/// than colliding on one hardcoded ID.
///
/// StartTime is always set server-side to the moment the reservation is
/// confirmed (immediate-use locker booking) — the client only picks how long
/// to hold the compartment for.
/// </summary>
public record CreateReservationRequest(
    Guid LockerId,
    string Size,
    int DurationHours,
    string IdempotencyKey);
