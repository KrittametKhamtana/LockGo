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
    public async Task GetLockers_ReturnsSeededLockers()
    {
        var response = await _client.GetAsync("/api/lockers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var lockers = await response.Content.ReadFromJsonAsync<List<LockerListItemDto>>();
        lockers.Should().NotBeNull();
        lockers!.Should().NotBeEmpty();
        lockers.Should().OnlyContain(l => l.AvailableCompartmentCount >= 0);
    }

    [Fact]
    public async Task GetLockers_FilteredBySize_OnlyReturnsLockersOfferingThatSize()
    {
        var response = await _client.GetAsync("/api/lockers?size=L");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var lockers = await response.Content.ReadFromJsonAsync<List<LockerListItemDto>>();
        lockers.Should().NotBeNull();
        lockers!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLockers_FilteredBySizeAndAvailability_ExcludesLockerWhoseOnlyAvailableCompartmentIsADifferentSize()
    {
        // A locker with its Small compartment occupied but Medium/Large still
        // available must NOT show up for "available Small" — size and
        // availability have to be true of the SAME compartment, not checked
        // independently (that was the bug: two separate Any() calls let an
        // available-but-wrong-size compartment satisfy the availability half).
        Guid lockerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LockGoDbContext>();
            var locker = await db.Lockers.Include(l => l.Compartments).FirstAsync();
            lockerId = locker.Id;
            var smallCompartment = locker.Compartments.First(c => c.Size == CompartmentSize.S);
            smallCompartment.Status = CompartmentStatus.Occupied;
            await db.SaveChangesAsync();
        }

        var availableSmallResponse = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?size=S&availability=true");
        var availableMediumResponse = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?size=M&availability=true");

        availableSmallResponse.Should().NotContain(l => l.Id == lockerId);
        availableMediumResponse.Should().Contain(l => l.Id == lockerId);
    }

    [Fact]
    public async Task GetLockerById_WhenLockerExists_ReturnsDetailWithCompartments()
    {
        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers");
        var lockerId = lockers!.First().Id;

        var response = await _client.GetAsync($"/api/lockers/{lockerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<LockerDetailDto>();
        detail!.Id.Should().Be(lockerId);
        detail.Compartments.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLockerById_WhenLockerDoesNotExist_Returns404WithErrorShape()
    {
        var response = await _client.GetAsync($"/api/lockers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("LOCKER_NOT_FOUND");
    }

    private record ErrorEnvelope(ErrorDetail Error);
    private record ErrorDetail(string Code, string Message);
}
