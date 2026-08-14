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
        var compartmentId = await GetAnyAvailableCompartmentIdAsync();
        var request = new CreateReservationRequest(compartmentId, DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var createResponse = await _client.PostAsJsonAsync("/api/reservations", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ReservationDto>();
        created!.CompartmentId.Should().Be(compartmentId);
        created.Status.Should().Be("Active");

        var getResponse = await _client.GetAsync($"/api/reservations/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ReservationDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.BookingNumber.Should().Be(created.BookingNumber);
    }

    [Fact]
    public async Task GetById_WhenReservationDoesNotExist_Returns404()
    {
        var response = await _client.GetAsync($"/api/reservations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ForNonexistentCompartment_Returns404WithErrorShape()
    {
        var request = new CreateReservationRequest(Guid.NewGuid(), DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var response = await _client.PostAsJsonAsync("/api/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("COMPARTMENT_NOT_FOUND");
    }

    [Fact]
    public async Task Create_WithOutOfRangeDuration_Returns400WithErrorShape()
    {
        var compartmentId = await GetAnyAvailableCompartmentIdAsync();
        var request = new CreateReservationRequest(compartmentId, DurationHours: 999, IdempotencyKey: Guid.NewGuid().ToString());

        var response = await _client.PostAsJsonAsync("/api/reservations", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        body!.Error.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Create_ReplayedWithSameIdempotencyKey_ReturnsTheSameReservationInstead()
    {
        var compartmentId = await GetAnyAvailableCompartmentIdAsync();
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateReservationRequest(compartmentId, DurationHours: 2, idempotencyKey);

        var first = await _client.PostAsJsonAsync("/api/reservations", request);
        var second = await _client.PostAsJsonAsync("/api/reservations", request);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstDto = await first.Content.ReadFromJsonAsync<ReservationDto>();
        var secondDto = await second.Content.ReadFromJsonAsync<ReservationDto>();
        secondDto!.Id.Should().Be(firstDto!.Id);
    }

    private async Task<Guid> GetAnyAvailableCompartmentIdAsync()
    {
        var lockers = await _client.GetFromJsonAsync<List<LockerListItemDto>>("/api/lockers?availability=true");
        var lockerDetail = await _client.GetFromJsonAsync<LockerDetailDto>($"/api/lockers/{lockers!.First().Id}");
        return lockerDetail!.Compartments.First(c => c.Status == "Available").Id;
    }

    private record ErrorEnvelope(ErrorDetail Error);
    private record ErrorDetail(string Code, string Message);
}
