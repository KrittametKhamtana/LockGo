using FluentAssertions;
using LockGo.Application.Common.Exceptions;
using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using LockGo.Application.Services;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;
using LockGo.Tests.TestSupport;
using Moq;

namespace LockGo.Tests.Unit;

public class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<ICompartmentRepository> _compartmentRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ReservationService _sut;

    public ReservationServiceTests()
    {
        // Executes the operation inline — the transactional/atomic guarantees
        // themselves are exercised separately in Concurrency/DoubleClickConfirmTests.
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<ReservationDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<ReservationDto>> op, CancellationToken ct) => op(ct));

        _sut = new ReservationService(_reservationRepository.Object, _compartmentRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenACompartmentOfThatSizeIsFree_CreatesReservation()
    {
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);

        NoExistingIdempotencyKey();
        _compartmentRepository
            .Setup(r => r.FindAvailableAsync(locker.Id, CompartmentSize.M, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartment);

        var request = new CreateReservationRequest(locker.Id, "M", DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.CompartmentId.Should().Be(compartment.Id);
        result.CompartmentSize.Should().Be("M");
        result.Status.Should().Be(nameof(ReservationStatus.Active));
        result.IsActive.Should().BeTrue();
        result.EndTime.Should().Be(result.StartTime.AddHours(2));
        _reservationRepository.Verify(r => r.Add(It.Is<Reservation>(res => res.CompartmentId == compartment.Id)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenEveryCompartmentOfThatSizeIsTaken_ThrowsConflict()
    {
        var locker = EntityFixtures.OpenLocker();

        NoExistingIdempotencyKey();
        NoAvailableCompartment();
        _compartmentRepository
            .Setup(r => r.ExistsForSizeAsync(locker.Id, CompartmentSize.M, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateReservationRequest(locker.Id, "M", DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request, CancellationToken.None));
        ex.Code.Should().Be("NO_AVAILABILITY");
        _reservationRepository.Verify(r => r.Add(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenLockerDoesNotOfferThatSizeAtAll_ThrowsNotFound()
    {
        var locker = EntityFixtures.OpenLocker();

        NoExistingIdempotencyKey();
        NoAvailableCompartment();
        _compartmentRepository
            .Setup(r => r.ExistsForSizeAsync(It.IsAny<Guid>(), It.IsAny<CompartmentSize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateReservationRequest(locker.Id, "L", DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(request, CancellationToken.None));
        ex.Code.Should().Be("COMPARTMENT_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_WhenOverlapIsActuallyOwnReplayWonByAConcurrentRequest_ReturnsThatReservationInsteadOfConflict()
    {
        // Reproduces a real READ COMMITTED race: a concurrent request with the SAME
        // idempotency key takes the last free compartment between our idempotency
        // check and our availability check, so the "no availability" we'd otherwise
        // report is really our own booking having already succeeded.
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);
        var wonByReplay = BuildReservation(compartment, "raced-key");

        _reservationRepository.SetupSequence(r => r.GetByIdempotencyKeyAsync("raced-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null) // first check: not yet committed by the other request
            .ReturnsAsync(wonByReplay);       // re-check after "no availability": now it has
        NoAvailableCompartment();

        var request = new CreateReservationRequest(locker.Id, "M", DurationHours: 2, IdempotencyKey: "raced-key");

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.Id.Should().Be(wonByReplay.Id);
        _reservationRepository.Verify(r => r.Add(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenLockerIsClosed_ThrowsConflict()
    {
        var locker = EntityFixtures.OpenLocker();
        locker.OperatingStatus = OperatingStatus.Closed;
        var compartment = EntityFixtures.AvailableCompartment(locker);

        NoExistingIdempotencyKey();
        _compartmentRepository
            .Setup(r => r.FindAvailableAsync(locker.Id, CompartmentSize.M, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartment);

        var request = new CreateReservationRequest(locker.Id, "M", DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request, CancellationToken.None));
        ex.Code.Should().Be("LOCKER_CLOSED");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(73)]
    public async Task CreateAsync_WhenDurationOutOfRange_ThrowsValidation(int durationHours)
    {
        var request = new CreateReservationRequest(Guid.NewGuid(), "M", durationHours, Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<ValidationAppException>(() => _sut.CreateAsync(request, CancellationToken.None));

        _reservationRepository.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSizeIsNotAValidCompartmentSize_ThrowsValidation()
    {
        var request = new CreateReservationRequest(Guid.NewGuid(), "XL", DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<ValidationAppException>(() => _sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenIdempotencyKeyAlreadyUsed_ReturnsExistingReservationWithoutCreatingANewOne()
    {
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);
        var existing = BuildReservation(compartment, "replayed-key");

        _reservationRepository.Setup(r => r.GetByIdempotencyKeyAsync("replayed-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateReservationRequest(locker.Id, "M", DurationHours: 2, IdempotencyKey: "replayed-key");

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.Id.Should().Be(existing.Id);
        result.BookingNumber.Should().Be(existing.BookingNumber);
        _compartmentRepository.Verify(
            r => r.FindAvailableAsync(It.IsAny<Guid>(), It.IsAny<CompartmentSize>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _reservationRepository.Verify(r => r.Add(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenReservationDoesNotExist_ThrowsNotFound()
    {
        _reservationRepository.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private void NoExistingIdempotencyKey() =>
        _reservationRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

    private void NoAvailableCompartment() =>
        _compartmentRepository
            .Setup(r => r.FindAvailableAsync(It.IsAny<Guid>(), It.IsAny<CompartmentSize>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Compartment?)null);

    private static Reservation BuildReservation(Compartment compartment, string idempotencyKey) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "LG-20260101-ABCDEF",
        UserId = Guid.NewGuid(),
        CompartmentId = compartment.Id,
        Compartment = compartment,
        StartTime = DateTimeOffset.UtcNow,
        EndTime = DateTimeOffset.UtcNow.AddHours(2),
        Status = ReservationStatus.Active,
        IdempotencyKey = idempotencyKey,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
