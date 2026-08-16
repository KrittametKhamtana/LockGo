using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LockGo.Application.DTOs;
using LockGo.Domain.Enums;
using LockGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LockGo.Tests.Integration;

public class LockersApiTests : IClassFixture<LockGoWebApplicationFactory>, IAsyncLifetime
{
    private readonly LockGoWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LockersApiTests(LockGoWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.SeedAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetLockers_ReturnsSeededLockersWithPerSizeAvailability()
    {
        var response = await _client.GetAsync("/api/lockers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var lockers = await response.Content.ReadFromJsonAsync<List<LockerListItemDto>>();
        lockers.Should().NotBeNull();
        lockers!.Should().NotBeEmpty();
        lockers.Should().OnlyContain(l => l.SizeAvailability.Count > 0);
        // Seed data deliberately gives some lockers several compartments of one size.
        lockers.Should().Contain(l => l.SizeAvailability.Any(s => s.TotalCount > 1));
    }

    [Fact]
    public async Task GetLockers_OmitsSizesTheLockerDoesNotOffer()
    {
        // Riverside is seeded with Small + Medium only — the UI relies on the
        // missing entry to show that Large simply isn't offered there.
        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?search=Riverside");

        var riverside = lockers.Should().ContainSingle().Subject;
        riverside.SizeAvailability.Select(s => s.Size).Should().BeEquivalentTo(["S", "M"]);
    }

    [Fact]
    public async Task GetLockers_SearchMatchesNameOrAddressCaseInsensitively()
    {
        var byName = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?search=chatuchak");
        // Two seeded lockers share "Silom" in their address — assert the match
        // is address-based and case-insensitive, not that it's unique.
        var byAddress = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?search=charoen nakhon");

        byName.Should().ContainSingle(l => l.Name.Contains("Chatuchak"));
        byAddress.Should().ContainSingle(l => l.Address.Contains("Charoen Nakhon"));
    }

    [Fact]
    public async Task GetLockers_SearchWithNoMatches_ReturnsEmptyList()
    {
        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?search=NoSuchPlaceExists");

        lockers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLockers_FilteredBySizeAndAvailability_ExcludesLockerWhoseOnlyAvailableCompartmentIsADifferentSize()
    {
        // Size and availability have to be true of the SAME compartment, not
        // checked independently — that was a real bug: two separate Any() calls
        // let an available-but-wrong-size compartment satisfy the availability half.
        int lockerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LockGoDbContext>();
            var locker = await db.Lockers
                .Include(l => l.Compartments)
                .FirstAsync(l => l.Compartments.Any(c => c.Size == CompartmentSize.S)
                              && l.Compartments.Any(c => c.Size == CompartmentSize.M));
            lockerId = locker.Id;

            // Book out every Small, leaving Medium free.
            foreach (var small in locker.Compartments.Where(c => c.Size == CompartmentSize.S))
            {
                db.Reservations.Add(BuildActiveReservation(small.Id));
            }

            await db.SaveChangesAsync();
        }

        var availableSmall = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?size=S&availability=true");
        var availableMedium = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?size=M&availability=true");

        availableSmall.Should().NotContain(l => l.Id == lockerId);
        availableMedium.Should().Contain(l => l.Id == lockerId);
    }

    [Fact]
    public async Task GetLockers_WhenEveryCompartmentIsBooked_ReportsFullyBookedButStillOpen()
    {
        int lockerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LockGoDbContext>();
            var locker = await db.Lockers
                .Include(l => l.Compartments)
                .FirstAsync(l => l.OperatingStatus == OperatingStatus.Open);
            lockerId = locker.Id;

            foreach (var compartment in locker.Compartments)
            {
                db.Reservations.Add(BuildActiveReservation(compartment.Id));
            }

            await db.SaveChangesAsync();
        }

        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers");
        var fullyBooked = lockers!.Single(l => l.Id == lockerId);

        fullyBooked.IsFullyBooked.Should().BeTrue();
        fullyBooked.AvailableCompartmentCount.Should().Be(0);
        // Fully booked is NOT the same as the site being closed.
        fullyBooked.OperatingStatus.Should().Be(nameof(OperatingStatus.Open));
        fullyBooked.SizeAvailability.Should().OnlyContain(s => s.AvailableCount == 0);
    }

    [Fact]
    public async Task GetLockers_WhenEveryCompartmentIsBooked_ExcludedByAvailabilityFilter()
    {
        int lockerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LockGoDbContext>();
            var locker = await db.Lockers
                .Include(l => l.Compartments)
                .FirstAsync(l => l.OperatingStatus == OperatingStatus.Open);
            lockerId = locker.Id;

            foreach (var compartment in locker.Compartments)
            {
                db.Reservations.Add(BuildActiveReservation(compartment.Id));
            }

            await db.SaveChangesAsync();
        }

        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?availability=true");

        lockers.Should().NotContain(l => l.Id == lockerId);
    }

    [Fact]
    public async Task GetLockers_ClosedLockerWithFreeCompartments_ExcludedByAvailabilityFilter()
    {
        // Airport Hub and Silom Complex are seeded Closed with unreserved
        // compartments — a locker can't be booked if the site itself is
        // closed, so "available only" must not surface it just because its
        // compartments individually look free.
        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?availability=true");

        lockers.Should().OnlyContain(l => l.OperatingStatus == nameof(OperatingStatus.Open));
    }

    [Fact]
    public async Task GetLockers_BookedOutNow_StillShowsAvailableForALaterWindow()
    {
        // The whole point of advance booking: "full" is a property of a time
        // window, not of the locker. Booking every compartment for the next few
        // hours must not hide the locker from someone searching for next week.
        int lockerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LockGoDbContext>();
            var locker = await db.Lockers
                .Include(l => l.Compartments)
                .FirstAsync(l => l.OperatingStatus == OperatingStatus.Open);
            lockerId = locker.Id;

            foreach (var compartment in locker.Compartments)
            {
                db.Reservations.Add(BuildActiveReservation(compartment.Id));
            }

            await db.SaveChangesAsync();
        }

        var now = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?availability=true");
        var nextWeek = DateTimeOffset.UtcNow.AddDays(7).ToString("O");
        var later = await _client.GetFromJsonAsync<List<LockerListItemDto>>(
            $"/api/lockers?availability=true&startTime={Uri.EscapeDataString(nextWeek)}&durationHours=2");

        now.Should().NotContain(l => l.Id == lockerId);
        later.Should().Contain(l => l.Id == lockerId);
    }

    [Fact]
    public async Task GetLockerById_ReportsAvailabilityForTheRequestedWindow_NotJustNow()
    {
        int lockerId;
        var start = DateTimeOffset.UtcNow.AddDays(2);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LockGoDbContext>();
            // Tests in this class share one in-memory database, so pick a locker
            // no earlier test has booked into — otherwise "free right now" is
            // already false before this test does anything.
            var locker = await db.Lockers
                .Include(l => l.Compartments)
                .FirstAsync(l => l.OperatingStatus == OperatingStatus.Open
                              && l.Compartments.Any()
                              && !l.Compartments.Any(c => c.Reservations.Any()));
            lockerId = locker.Id;

            // Booked for a window two days out — invisible to a "right now" query.
            foreach (var compartment in locker.Compartments)
            {
                var reservation = BuildActiveReservation(compartment.Id);
                reservation.StartTime = start;
                reservation.EndTime = start.AddHours(4);
                db.Reservations.Add(reservation);
            }

            await db.SaveChangesAsync();
        }

        var rightNow = await _client.GetFromJsonAsync<LockerDetailDto>($"/api/lockers/{lockerId}");
        var thatWindow = await _client.GetFromJsonAsync<LockerDetailDto>(
            $"/api/lockers/{lockerId}?startTime={Uri.EscapeDataString(start.ToString("O"))}&durationHours=2");

        rightNow!.IsFullyBooked.Should().BeFalse();
        thatWindow!.IsFullyBooked.Should().BeTrue();
    }

    [Fact]
    public async Task GetLockerById_WhenLockerExists_ReturnsPerSizeAvailability()
    {
        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers");
        var lockerId = lockers!.First().Id;

        var response = await _client.GetAsync($"/api/lockers/{lockerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<LockerDetailDto>();
        detail!.Id.Should().Be(lockerId);
        detail.SizeAvailability.Should().NotBeEmpty();
        detail.SizeAvailability.Should().OnlyContain(s => s.TotalCount > 0);
    }

    [Fact]
    public async Task GetLockerById_WhenLockerDoesNotExist_Returns404WithErrorShape()
    {
        var response = await _client.GetAsync("/api/lockers/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("LOCKER_NOT_FOUND");
    }

    private static Domain.Entities.Reservation BuildActiveReservation(int compartmentId) => new()
    {
        Id = Random.Shared.Next(1, int.MaxValue),
        BookingNumber = $"LG-TEST-{Guid.NewGuid():N}"[..20],
        UserId = Application.Common.MockUser.Id,
        CompartmentId = compartmentId,
        StartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
        EndTime = DateTimeOffset.UtcNow.AddHours(4),
        Status = ReservationStatus.Active,
        IdempotencyKey = Guid.NewGuid().ToString(),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private record ErrorEnvelope(ErrorDetail Error);
    private record ErrorDetail(string Code, string Message);
}
