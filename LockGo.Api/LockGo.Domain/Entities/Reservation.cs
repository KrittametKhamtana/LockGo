using LockGo.Domain.Enums;

namespace LockGo.Domain.Entities;

public class Reservation
{
    public int Id { get; set; }

    /// <summary>
    /// Unique, human-readable booking reference (e.g. LG-20260814-AB3F). This is
    /// the public lookup key — <see cref="Id"/> is sequential and must never
    /// appear in a URL, or anyone could enumerate other people's bookings.
    /// </summary>
    public string BookingNumber { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CompartmentId { get; set; }
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
