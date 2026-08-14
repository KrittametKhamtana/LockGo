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
    public async Task CreateAsync_WhenCompartmentIsAvailable_CreatesReservation()
    {
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);

        _reservationRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);
        _compartmentRepository.Setup(r => r.GetByIdWithLockerAsync(compartment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartment);
        _reservationRepository.Setup(r => r.HasOverlapAsync(compartment.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateReservationRequest(compartment.Id, DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.CompartmentId.Should().Be(compartment.Id);
        result.Status.Should().Be(nameof(ReservationStatus.Active));
        result.IsActive.Should().BeTrue();
        result.EndTime.Should().Be(result.StartTime.AddHours(2));
        _reservationRepository.Verify(r => r.Add(It.Is<Reservation>(res => res.CompartmentId == compartment.Id)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCompartmentAlreadyBookedForOverlappingTime_ThrowsConflict_PreventsDoubleBooking()
    {
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);

        _reservationRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);
        _compartmentRepository.Setup(r => r.GetByIdWithLockerAsync(compartment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartment);
        _reservationRepository.Setup(r => r.HasOverlapAsync(compartment.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateReservationRequest(compartment.Id, DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var act = () => _sut.CreateAsync(request, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(act);
        ex.Code.Should().Be("NO_AVAILABILITY");
        _reservationRepository.Verify(r => r.Add(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenOverlapIsActuallyOwnReplayWonByAConcurrentRequest_ReturnsThatReservationInsteadOfConflict()
    {
        // Reproduces a real READ COMMITTED race: a concurrent request with the SAME
        // idempotency key commits between our idempotency check and our overlap
        // check, so the "overlap" we see is our own replay, not a competing booking.
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);
        var wonByReplay = new Reservation
        {
            Id = Guid.NewGuid(),
            BookingNumber = "LG-20260101-ABCDEF",
            UserId = Guid.NewGuid(),
            CompartmentId = compartment.Id,
            Compartment = compartment,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(2),
            Status = ReservationStatus.Active,
            IdempotencyKey = "raced-key",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _reservationRepository.SetupSequence(r => r.GetByIdempotencyKeyAsync("raced-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null) // first check: not yet committed by the other request
            .ReturnsAsync(wonByReplay);       // re-check after "overlap": now it has committed
        _compartmentRepository.Setup(r => r.GetByIdWithLockerAsync(compartment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartment);
        _reservationRepository.Setup(r => r.HasOverlapAsync(compartment.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateReservationRequest(compartment.Id, DurationHours: 2, IdempotencyKey: "raced-key");

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.Id.Should().Be(wonByReplay.Id);
        _reservationRepository.Verify(r => r.Add(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCompartmentDoesNotExist_ThrowsNotFound()
    {
        _reservationRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);
        _compartmentRepository.Setup(r => r.GetByIdWithLockerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Compartment?)null);

        var request = new CreateReservationRequest(Guid.NewGuid(), DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenLockerIsClosed_ThrowsConflict()
    {
        var locker = EntityFixtures.OpenLocker();
        locker.OperatingStatus = OperatingStatus.Closed;
        var compartment = EntityFixtures.AvailableCompartment(locker);

        _reservationRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);
        _compartmentRepository.Setup(r => r.GetByIdWithLockerAsync(compartment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartment);

        var request = new CreateReservationRequest(compartment.Id, DurationHours: 2, IdempotencyKey: Guid.NewGuid().ToString());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request, CancellationToken.None));
        ex.Code.Should().Be("LOCKER_CLOSED");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(73)]
    public async Task CreateAsync_WhenDurationOutOfRange_ThrowsValidation(int durationHours)
    {
        var request = new CreateReservationRequest(Guid.NewGuid(), durationHours, Guid.NewGuid().ToString());

        await Assert.ThrowsAsync<ValidationAppException>(() => _sut.CreateAsync(request, CancellationToken.None));

        _reservationRepository.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenIdempotencyKeyAlreadyUsed_ReturnsExistingReservationWithoutCreatingANewOne()
    {
        var locker = EntityFixtures.OpenLocker();
        var compartment = EntityFixtures.AvailableCompartment(locker);
        var existing = new Reservation
        {
            Id = Guid.NewGuid(),
            BookingNumber = "LG-20260101-ABCDEF",
            UserId = Guid.NewGuid(),
            CompartmentId = compartment.Id,
            Compartment = compartment,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(2),
            Status = ReservationStatus.Active,
            IdempotencyKey = "replayed-key",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _reservationRepository.Setup(r => r.GetByIdempotencyKeyAsync("replayed-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new CreateReservationRequest(compartment.Id, DurationHours: 2, IdempotencyKey: "replayed-key");

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        result.Id.Should().Be(existing.Id);
        result.BookingNumber.Should().Be(existing.BookingNumber);
        _compartmentRepository.Verify(r => r.GetByIdWithLockerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _reservationRepository.Verify(r => r.Add(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenReservationDoesNotExist_ThrowsNotFound()
    {
        _reservationRepository.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
