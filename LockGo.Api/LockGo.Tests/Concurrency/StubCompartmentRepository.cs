using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;

namespace LockGo.Tests.Concurrency;

public class StubCompartmentRepository : ICompartmentRepository
{
    private readonly Compartment _compartment;

    public StubCompartmentRepository(Compartment compartment)
    {
        _compartment = compartment;
    }

    public Task<Compartment?> GetByIdWithLockerAsync(Guid id, CancellationToken ct)
        => Task.FromResult(id == _compartment.Id ? _compartment : null);
}
