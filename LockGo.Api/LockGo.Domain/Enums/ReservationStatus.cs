namespace LockGo.Domain.Enums;

/// <summary>
/// No separate "Expired" state — expiry is always derived from EndTime,
/// see <see cref="Entities.Reservation.IsActive"/>.
/// </summary>
public enum ReservationStatus
{
    Active,
    Completed,
    Cancelled
}
