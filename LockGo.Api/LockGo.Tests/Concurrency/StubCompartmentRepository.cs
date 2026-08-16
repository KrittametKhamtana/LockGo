using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;

namespace LockGo.Tests.Concurrency;

/// <summary>
/// Backs a fixed pool of compartments for one locker+size. Hands out the first
/// one not already claimed by an active reservation in the shared
/// RacyReservationRepository, mirroring what the real SQL
/// "no overlapping active reservation" predicate does — so the concurrency
/// tests exercise a real "last compartment wins" race, not a stub that always
/// returns the same instance.
/// </summary>
public class StubCompartmentRepository : ICompartmentRepository
{
    private readonly IReadOnlyList<Compartment> _compartments;
    private readonly RacyReservationRepository _reservations;

    public StubCompartmentRepository(RacyReservationRepository reservations, params Compartment[] compartments)
    {
        _reservations = reservations;
        _compartments = compartments;
    }

    public Task<Compartment?> FindAvailableAsync(
        int lockerId,
        CompartmentSize size,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        var match = _compartments.FirstOrDefault(c =>
            c.LockerId == lockerId &&
            c.Size == size &&
            !_reservations.HasOverlap(c.Id, start, end));

        return Task.FromResult(match);
    }

    public Task<bool> ExistsForSizeAsync(int lockerId, CompartmentSize size, CancellationToken ct)
        => Task.FromResult(_compartments.Any(c => c.LockerId == lockerId && c.Size == size));
}
