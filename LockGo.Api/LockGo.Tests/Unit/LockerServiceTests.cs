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
    private readonly Mock<ILockerRepository> _lockerRepository = new();

    [Fact]
    public async Task SearchAsync_ReportsAvailabilityPerSize_IncludingSizesTheLockerDoesNotOffer()
    {
        // 3 Small (1 booked) and 2 Medium (both free); no Large at all.
        var locker = EntityFixtures.OpenLocker();
        var smalls = EntityFixtures.AvailableCompartments(locker, CompartmentSize.S, 3, price: 20m);
        var mediums = EntityFixtures.AvailableCompartments(locker, CompartmentSize.M, 2, price: 35m);
        BookCompartment(smalls[0]);
        locker.Compartments = smalls.Concat(mediums).ToList();

        var results = await SearchAsync(locker, new LockerSearchQuery(null, null, null, null, null, null));

        var result = results.Should().ContainSingle().Subject;
        result.SizeAvailability.Should().HaveCount(2);

        var small = result.SizeAvailability.Single(s => s.Size == "S");
        small.AvailableCount.Should().Be(2);
        small.TotalCount.Should().Be(3);
        small.Price.Should().Be(20m);

        var medium = result.SizeAvailability.Single(s => s.Size == "M");
        medium.AvailableCount.Should().Be(2);
        medium.TotalCount.Should().Be(2);

        result.SizeAvailability.Should().NotContain(s => s.Size == "L");
        result.IsFullyBooked.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_WhenEveryCompartmentIsBooked_MarksLockerFullyBookedWithoutChangingOperatingStatus()
    {
        var locker = EntityFixtures.OpenLocker();
        var compartments = EntityFixtures.AvailableCompartments(locker, CompartmentSize.S, 2, price: 20m);
        compartments.ForEach(BookCompartment);
        locker.Compartments = compartments;

        var results = await SearchAsync(locker, new LockerSearchQuery(null, null, null, null, null, null));

        var result = results.Should().ContainSingle().Subject;
        result.IsFullyBooked.Should().BeTrue();
        result.AvailableCompartmentCount.Should().Be(0);
        // "Fully booked" is a separate concept from the site being closed.
        result.OperatingStatus.Should().Be(nameof(OperatingStatus.Open));
    }

    [Fact]
    public async Task SearchAsync_IgnoresExpiredReservations_WhenDerivingAvailability()
    {
        // Expiry is lazy — an Active reservation whose EndTime has passed must not
        // keep counting against availability (no cron job sweeps these).
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker, CompartmentSize.S, price: 20m);
        compartment.Reservations = new List<Reservation>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CompartmentId = compartment.Id,
                Status = ReservationStatus.Active,
                StartTime = DateTimeOffset.UtcNow.AddHours(-5),
                EndTime = DateTimeOffset.UtcNow.AddHours(-1),
            },
        };
        locker.Compartments = new List<Compartment> { compartment };

        var results = await SearchAsync(locker, new LockerSearchQuery(null, null, null, null, null, null));

        results.Single().SizeAvailability.Single().AvailableCount.Should().Be(1);
        results.Single().IsFullyBooked.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_WhenFilteredBySize_ReportsHeadlinePriceAndCountForThatSizeOnly()
    {
        // Without size-scoping, minPrice would report the cheaper Small even
        // though the caller explicitly asked for Large.
        var locker = EntityFixtures.OpenLocker();
        var small = EntityFixtures.AvailableCompartment(locker, CompartmentSize.S, price: 20m);
        var large = EntityFixtures.AvailableCompartment(locker, CompartmentSize.L, price: 50m);
        BookCompartment(large);
        locker.Compartments = new List<Compartment> { small, large };

        var results = await SearchAsync(locker, new LockerSearchQuery(null, null, null, "L", null, null));

        var result = results.Should().ContainSingle().Subject;
        result.MinPrice.Should().Be(50m);
        result.AvailableCompartmentCount.Should().Be(0);
        // The full per-size breakdown is still returned regardless of the filter.
        result.SizeAvailability.Should().HaveCount(2);
    }

    private async Task<IReadOnlyList<LockerListItemDto>> SearchAsync(Locker locker, LockerSearchQuery query)
    {
        _lockerRepository.Setup(r => r.SearchAsync(It.IsAny<LockerSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Locker> { locker });

        return await new LockerService(_lockerRepository.Object).SearchAsync(query, CancellationToken.None);
    }

    private static void BookCompartment(Compartment compartment)
    {
        compartment.Status = CompartmentStatus.Occupied;
        compartment.Reservations = new List<Reservation>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CompartmentId = compartment.Id,
                Status = ReservationStatus.Active,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddHours(2),
            },
        };
    }
}
