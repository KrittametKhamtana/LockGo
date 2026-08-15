using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LockGo.Application.DTOs;

namespace LockGo.Tests.Integration;

public class ReservationsApiTests : IClassFixture<LockGoWebApplicationFactory>, IAsyncLifetime
{
    private readonly LockGoWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReservationsApiTests(LockGoWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.SeedAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_ThenGetById_ReturnsTheSameReservation()
    {
        var (lockerId, size) = await GetLockerWithAvailableSizeAsync();
        var request = new CreateReservationRequest(lockerId, size, DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString(), StartTime: DateTimeOffset.UtcNow);

        var createResponse = await _client.PostAsJsonAsync("/api/reservations", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ReservationDto>();
        created!.LockerId.Should().Be(lockerId);
        created.CompartmentSize.Should().Be(size);
        created.Status.Should().Be("Active");

        var getResponse = await _client.GetAsync($"/api/reservations/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ReservationDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.BookingNumber.Should().Be(created.BookingNumber);
    }

    [Fact]
    public async Task Create_DecrementsThatSizesAvailabilityByOne_LeavingOtherSizesUntouched()
    {
        var (lockerId, size) = await GetLockerWithAvailableSizeAsync(minAvailable: 2);
        var before = await GetSizeAvailabilityAsync(lockerId, size);

        var request = new CreateReservationRequest(lockerId, size, DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString(), StartTime: DateTimeOffset.UtcNow);
        await _client.PostAsJsonAsync("/api/reservations", request);

        var after = await GetSizeAvailabilityAsync(lockerId, size);
        after.AvailableCount.Should().Be(before.AvailableCount - 1);
        // The compartment still exists — only its availability changed.
        after.TotalCount.Should().Be(before.TotalCount);
    }

    [Fact]
    public async Task GetById_WhenReservationDoesNotExist_Returns404()
    {
        var response = await _client.GetAsync($"/api/reservations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ForNonexistentLocker_Returns404WithErrorShape()
    {
        var request = new CreateReservationRequest(Guid.NewGuid(), "M", DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString(), StartTime: DateTimeOffset.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("COMPARTMENT_NOT_FOUND");
    }

    [Fact]
    public async Task Create_ForSizeTheLockerDoesNotOffer_Returns404()
    {
        // Riverside is seeded with Small + Medium only.
        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?search=Riverside");
        var riverside = lockers!.Single();
        riverside.SizeAvailability.Should().NotContain(s => s.Size == "L");

        var request = new CreateReservationRequest(riverside.Id, "L", DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString(), StartTime: DateTimeOffset.UtcNow);
        var response = await _client.PostAsJsonAsync("/api/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithOutOfRangeDuration_Returns400WithErrorShape()
    {
        var (lockerId, size) = await GetLockerWithAvailableSizeAsync();
        var request = new CreateReservationRequest(lockerId, size, DurationHours: 999, IdempotencyKey: Guid.NewGuid().ToString(), StartTime: DateTimeOffset.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Create_ReplayedWithSameIdempotencyKey_ReturnsTheSameReservationInstead()
    {
        var (lockerId, size) = await GetLockerWithAvailableSizeAsync(minAvailable: 2);
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateReservationRequest(lockerId, size, DurationHours: 2, idempotencyKey, StartTime: DateTimeOffset.UtcNow);

        var first = await _client.PostAsJsonAsync("/api/reservations", request);
        var second = await _client.PostAsJsonAsync("/api/reservations", request);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstDto = await first.Content.ReadFromJsonAsync<ReservationDto>();
        var secondDto = await second.Content.ReadFromJsonAsync<ReservationDto>();
        // Critically: the replay must NOT consume a second compartment even
        // though one was available.
        secondDto!.Id.Should().Be(firstDto!.Id);
        secondDto.CompartmentId.Should().Be(firstDto.CompartmentId);
    }

    private async Task<(Guid LockerId, string Size)> GetLockerWithAvailableSizeAsync(int minAvailable = 1)
    {
        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?availability=true");
        var locker = lockers!.First(l =>
            l.OperatingStatus == "Open" &&
            l.SizeAvailability.Any(s => s.AvailableCount >= minAvailable));

        return (locker.Id, locker.SizeAvailability.First(s => s.AvailableCount >= minAvailable).Size);
    }

    private async Task<CompartmentSizeAvailabilityDto> GetSizeAvailabilityAsync(Guid lockerId, string size)
    {
        var detail = await _client.GetFromJsonAsync<LockerDetailDto>($"/api/lockers/{lockerId}");
        return detail!.SizeAvailability.Single(s => s.Size == size);
    }

    private record ErrorEnvelope(ErrorDetail Error);
    private record ErrorDetail(string Code, string Message);
}
