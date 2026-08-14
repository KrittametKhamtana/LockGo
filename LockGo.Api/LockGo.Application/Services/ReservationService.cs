using LockGo.Application.Common;
using LockGo.Application.Common.Exceptions;
using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;

namespace LockGo.Application.Services;

public class ReservationService : IReservationService
{
    private const int MinDurationHours = 1;
    private const int MaxDurationHours = 72;

    private readonly IReservationRepository _reservationRepository;
    private readonly ICompartmentRepository _compartmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReservationService(
        IReservationRepository reservationRepository,
        ICompartmentRepository compartmentRepository,
        IUnitOfWork unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _compartmentRepository = compartmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReservationDto> CreateAsync(CreateReservationRequest request, CancellationToken ct)
    {
        if (request.DurationHours is < MinDurationHours or > MaxDurationHours)
        {
            throw new ValidationAppException($"DurationHours must be between {MinDurationHours} and {MaxDurationHours}.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ValidationAppException("IdempotencyKey is required.");
        }

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(ct2 => CreateInTransactionAsync(request, ct2), ct);
        }
        catch (IdempotencyKeyConflictException)
        {
            // Two requests raced past the check-then-insert window (rapid double-click
            // hitting different app instances); the unique constraint caught the second
            // insert. Re-read and return the winning reservation — still idempotent from
            // the caller's point of view.
            var existing = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct)
                ?? throw new InvalidOperationException(
                    "Idempotency key conflict reported but no matching reservation was found.");
            return MapToDto(existing);
        }
    }

    private async Task<ReservationDto> CreateInTransactionAsync(CreateReservationRequest request, CancellationToken ct)
    {
        var existing = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
        if (existing is not null)
        {
            return MapToDto(existing);
        }

        var compartment = await _compartmentRepository.GetByIdWithLockerAsync(request.CompartmentId, ct)
            ?? throw new NotFoundException("COMPARTMENT_NOT_FOUND", $"Compartment '{request.CompartmentId}' was not found.");

        if (compartment.Locker.OperatingStatus != OperatingStatus.Open)
        {
            throw new ConflictException("LOCKER_CLOSED", "This locker is currently closed.");
        }

        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime.AddHours(request.DurationHours);

        // Authoritative check — never trust the denormalized Compartment.Status here.
        var hasOverlap = await _reservationRepository.HasOverlapAsync(compartment.Id, startTime, endTime, ct);
        if (hasOverlap)
        {
            // Race window: under READ COMMITTED, a concurrent request sharing this
            // exact IdempotencyKey can commit its reservation in the gap between our
            // idempotency check above and this overlap check — we'd then "overlap"
            // with our own replay rather than a competing booking. Re-check before
            // concluding this is a real conflict.
            var wonByReplay = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            if (wonByReplay is not null)
            {
                return MapToDto(wonByReplay);
            }

            throw new ConflictException("NO_AVAILABILITY", "This compartment is not available for the selected time.");
        }

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            BookingNumber = BookingNumberGenerator.Generate(),
            UserId = MockUser.Id,
            CompartmentId = compartment.Id,
            StartTime = startTime,
            EndTime = endTime,
            Status = ReservationStatus.Active,
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _reservationRepository.Add(reservation);
        compartment.Status = CompartmentStatus.Occupied;

        reservation.Compartment = compartment;
        return MapToDto(reservation);
    }

    public async Task<ReservationDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var reservation = await _reservationRepository.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException("RESERVATION_NOT_FOUND", $"Reservation '{id}' was not found.");

        return MapToDto(reservation);
    }

    private static ReservationDto MapToDto(Reservation reservation) => new(
        reservation.Id,
        reservation.BookingNumber,
        reservation.Compartment.LockerId,
        reservation.Compartment.Locker.Name,
        reservation.Compartment.Locker.Address,
        reservation.CompartmentId,
        reservation.Compartment.Size.ToString(),
        reservation.Compartment.Price,
        reservation.StartTime,
        reservation.EndTime,
        reservation.Status.ToString(),
        reservation.IsActive);
}
