using LockGo.Domain.Entities;
using LockGo.Domain.Enums;

namespace LockGo.Tests.TestSupport;

public static class EntityFixtures
{
    // Ids are identity columns in the real schema, so nothing assigns them in
    // production code. Fixtures still need distinct ones to tell entities apart,
    // and they must stay unique across a whole test run (xUnit runs classes in
    // parallel), hence the interlocked counter rather than a per-fixture reset.
    private static int _nextId;

    private static int NextId() => Interlocked.Increment(ref _nextId);

    public static Locker OpenLocker(string name = "Test Locker") => new()
    {
        Id = NextId(),
        Name = name,
        Address = "1 Test Street",
        Lat = 13.7278,
        Lng = 100.5241,
        OperatingStatus = OperatingStatus.Open,
    };

    public static Compartment AvailableCompartment(Locker locker, CompartmentSize size = CompartmentSize.M, decimal price = 35m) => new()
    {
        Id = NextId(),
        LockerId = locker.Id,
        Locker = locker,
        Size = size,
        Price = price,
        Status = CompartmentStatus.Available,
    };

    /// <summary>Several compartments of one size in the same locker — the case that makes "Small: 2 left" meaningful.</summary>
    public static List<Compartment> AvailableCompartments(Locker locker, CompartmentSize size, int count, decimal price = 35m) =>
        Enumerable.Range(0, count).Select(_ => AvailableCompartment(locker, size, price)).ToList();
}
