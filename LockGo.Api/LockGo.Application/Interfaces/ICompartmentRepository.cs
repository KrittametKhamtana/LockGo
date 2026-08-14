using LockGo.Domain.Entities;

namespace LockGo.Application.Interfaces;

public interface ICompartmentRepository
{
    /// <summary>Includes the parent Locker so callers can build a ReservationDto without a second round-trip.</summary>
    Task<Compartment?> GetByIdWithLockerAsync(Guid id, CancellationToken ct);
}
