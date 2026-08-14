using FluentAssertions;
using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using LockGo.Application.Services;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;
using LockGo.Tests.TestSupport;
using Moq;

namespace LockGo.Tests.Unit;

public class LockerServiceTests
{
    [Fact]
    public async Task SearchAsync_WhenFilteredBySize_ReportsPriceAndAvailabilityForThatSizeOnly()
    {
        // Without the size filter, minPrice/availableCompartmentCount would
        // reflect the Small compartment (cheapest, available) even though the
        // caller specifically asked for Large — misleading the exact way the
        // combined size+availability search bug did.
        var locker = EntityFixtures.OpenLocker();
        var small = EntityFixtures.AvailableCompartment(locker, CompartmentSize.S, price: 20m);
        var large = new Compartment { Id = Guid.NewGuid(), LockerId = locker.Id, Locker = locker, Size = CompartmentSize.L, Price = 50m, Status = CompartmentStatus.Occupied };
        locker.Compartments = new List<Compartment> { small, large };

        var repository = new Mock<ILockerRepository>();
        repository.Setup(r => r.SearchAsync(It.IsAny<LockerSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Locker> { locker });

        var sut = new LockerService(repository.Object);

        var results = await sut.SearchAsync(new LockerSearchQuery(null, null, null, "L", null), CancellationToken.None);

        var result = results.Should().ContainSingle().Subject;
        result.MinPrice.Should().Be(50m);
        result.AvailableCompartmentCount.Should().Be(0);
    }
}
