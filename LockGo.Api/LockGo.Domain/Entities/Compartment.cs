using LockGo.Domain.Enums;

namespace LockGo.Domain.Entities;

public class Compartment
{
    public Guid Id { get; set; }
    public Guid LockerId { get; set; }
    public Locker Locker { get; set; } = null!;

    public CompartmentSize Size { get; set; }
    public decimal Price { get; set; }

    /// <summary>
    /// Denormalized fast-read status. NOT the source of truth on write —
    /// the booking path re-checks live Reservation rows inside the transaction.
    /// Concurrency is guarded by Postgres' xmin system column (configured in
    /// Infrastructure), not by a mapped property here.
    /// </summary>
    public CompartmentStatus Status { get; set; } = CompartmentStatus.Available;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
