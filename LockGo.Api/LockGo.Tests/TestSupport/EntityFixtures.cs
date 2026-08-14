using LockGo.Domain.Entities;
using LockGo.Domain.Enums;

namespace LockGo.Tests.TestSupport;

public static class EntityFixtures
{
    public static Locker OpenLocker(string name = "Test Locker") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Address = "1 Test Street",
        Lat = 13.7278,
        Lng = 100.5241,
        OperatingStatus = OperatingStatus.Open,
    };

    public static Compartment AvailableCompartment(Locker locker, CompartmentSize size = CompartmentSize.M, decimal price = 35m) => new()
    {
        Id = Guid.NewGuid(),
        LockerId = locker.Id,
        Locker = locker,
        Size = size,
        Price = price,
        Status = CompartmentStatus.Available,
    };
}
