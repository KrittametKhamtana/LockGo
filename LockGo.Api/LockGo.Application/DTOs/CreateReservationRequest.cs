namespace LockGo.Application.DTOs;

/// <summary>
/// The client books a locker + compartment size; the server assigns a
/// specific free compartment of that size inside the transaction. Picking the
/// compartment server-side is what makes "Small has 2 free" meaningful —
/// two users choosing "Small" concurrently get different compartments rather
/// than colliding on one hardcoded ID.
///
/// StartTime is client-supplied (advance booking, not just immediate-use) —
/// the server only checks it's a sane value (not in the past, not too far
/// out) and uses it as-is for the availability-overlap check.
/// </summary>
public record CreateReservationRequest(
    int LockerId,
    string Size,
    int DurationHours,
    string IdempotencyKey,
    DateTimeOffset StartTime);
