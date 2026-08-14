using LockGo.Domain.Enums;

namespace LockGo.Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; }

    /// <summary>Unique, human-readable booking reference (e.g. LG-20260814-AB3F).</summary>
    public string BookingNumber { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CompartmentId { get; set; }
    public Compartment Compartment { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    /// <summary>No separate "Expired" state — see <see cref="IsActive"/>.</summary>
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;

    /// <summary>Client-generated key that makes POST /reservations safe to retry/double-click.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Effective active status is always derived, never stored as "expired".</summary>
    public bool IsActive => Status == ReservationStatus.Active && EndTime > DateTimeOffset.UtcNow;
}
