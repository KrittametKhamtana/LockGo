using FluentAssertions;
using LockGo.Application.DTOs;
using LockGo.Application.Services;
using LockGo.Tests.TestSupport;

namespace LockGo.Tests.Concurrency;

/// <summary>
/// Debugging challenge: proves that rapidly double-clicking Confirm — two
/// requests carrying the SAME client-generated idempotencyKey, genuinely
/// concurrent, not sequential — never creates two reservations.
/// </summary>
public class DoubleClickConfirmTests
{
    [Fact]
    public async Task CreateAsync_CalledConcurrentlyWithSameIdempotencyKey_CreatesExactlyOneReservation()
    {
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);

        var repository = new RacyReservationRepository(expectedConcurrentCallers: 2);
        var service = new ReservationService(repository, new StubCompartmentRepository(compartment), new PassthroughUnitOfWork());

        var request = new CreateReservationRequest(compartment.Id, DurationHours: 2, IdempotencyKey: "double-click-key");

        // Genuinely concurrent — not two sequential awaits. This is what
        // reproduces the race a fast double-click on Confirm creates.
        var results = await Task.WhenAll(
            service.CreateAsync(request, CancellationToken.None),
            service.CreateAsync(request, CancellationToken.None));

        repository.InsertedCount.Should().Be(1);
        results[0].Id.Should().Be(results[1].Id);
        results[0].BookingNumber.Should().Be(results[1].BookingNumber);
    }

    [Fact]
    public async Task CreateAsync_CalledManyTimesConcurrentlyWithSameIdempotencyKey_StillCreatesExactlyOneReservation()
    {
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);

        const int concurrentCallers = 20;
        var repository = new RacyReservationRepository(expectedConcurrentCallers: concurrentCallers);
        var service = new ReservationService(repository, new StubCompartmentRepository(compartment), new PassthroughUnitOfWork());

        var request = new CreateReservationRequest(compartment.Id, DurationHours: 2, IdempotencyKey: "hammered-key");

        var tasks = Enumerable.Range(0, concurrentCallers).Select(_ => service.CreateAsync(request, CancellationToken.None));
        var results = await Task.WhenAll(tasks);

        repository.InsertedCount.Should().Be(1);
        results.Select(r => r.Id).Distinct().Should().ContainSingle();
    }
}
