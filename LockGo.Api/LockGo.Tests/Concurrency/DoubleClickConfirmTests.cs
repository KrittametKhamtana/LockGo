using FluentAssertions;
using LockGo.Application.Common.Exceptions;
using LockGo.Application.DTOs;
using LockGo.Application.Services;
using LockGo.Domain.Enums;
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
        // Two free compartments of the same size: without idempotency handling the
        // second click would happily take the *other* one and double-book the user.
        var compartments = EntityFixtures.AvailableCompartments(locker, CompartmentSize.S, 2, price: 20m);

        var repository = new RacyReservationRepository(expectedConcurrentCallers: 2);
        var service = new ReservationService(
            repository,
            new StubCompartmentRepository(repository, compartments.ToArray()),
            new PassthroughUnitOfWork());

        var request = new CreateReservationRequest(locker.Id, "S", DurationHours: 2, IdempotencyKey: "double-click-key", StartTime: DateTimeOffset.UtcNow);

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
        const int concurrentCallers = 20;
        var locker = EntityFixtures.OpenLocker();
        var compartments = EntityFixtures.AvailableCompartments(locker, CompartmentSize.S, concurrentCallers, price: 20m);

        var repository = new RacyReservationRepository(expectedConcurrentCallers: concurrentCallers);
        var service = new ReservationService(
            repository,
            new StubCompartmentRepository(repository, compartments.ToArray()),
            new PassthroughUnitOfWork());

        var request = new CreateReservationRequest(locker.Id, "S", DurationHours: 2, IdempotencyKey: "hammered-key", StartTime: DateTimeOffset.UtcNow);

        var tasks = Enumerable.Range(0, concurrentCallers).Select(_ => service.CreateAsync(request, CancellationToken.None));
        var results = await Task.WhenAll(tasks);

        repository.InsertedCount.Should().Be(1);
        results.Select(r => r.Id).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAsync_ConcurrentDistinctRequestsForTheSameSize_ConsumeSeparateCompartmentsThenRunOut()
    {
        // Different users (different idempotency keys) booking the same size
        // concurrently must each get their own compartment — and once the pool is
        // exhausted the rest must be rejected, not silently double-booked.
        const int compartmentCount = 3;
        const int callerCount = 5;

        var locker = EntityFixtures.OpenLocker();
        var compartments = EntityFixtures.AvailableCompartments(locker, CompartmentSize.S, compartmentCount, price: 20m);

        var repository = new RacyReservationRepository(expectedConcurrentCallers: callerCount);
        var service = new ReservationService(
            repository,
            new StubCompartmentRepository(repository, compartments.ToArray()),
            new PassthroughUnitOfWork());

        var attempts = Enumerable.Range(0, callerCount).Select(i =>
            Task.Run(async () =>
            {
                try
                {
                    var request = new CreateReservationRequest(locker.Id, "S", DurationHours: 2, IdempotencyKey: $"user-{i}", StartTime: DateTimeOffset.UtcNow);
                    return (Reservation: await service.CreateAsync(request, CancellationToken.None), Conflict: false);
                }
                catch (ConflictException)
                {
                    return (Reservation: (ReservationDto?)null, Conflict: true)!;
                }
            }));

        var outcomes = await Task.WhenAll(attempts);

        var booked = outcomes.Where(o => !o.Conflict).Select(o => o.Reservation!).ToList();
        booked.Should().HaveCount(compartmentCount);
        booked.Select(r => r.CompartmentId).Distinct().Should().HaveCount(compartmentCount);
        outcomes.Count(o => o.Conflict).Should().Be(callerCount - compartmentCount);
        repository.InsertedCount.Should().Be(compartmentCount);
    }
}
