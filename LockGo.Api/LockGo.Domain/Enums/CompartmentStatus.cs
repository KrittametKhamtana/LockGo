namespace LockGo.Domain.Enums;

/// <summary>
/// Denormalized fast-read status. Not the source of truth on write —
/// booking writes must re-check live Reservation rows instead.
/// </summary>
public enum CompartmentStatus
{
    Available,
    Occupied
}
