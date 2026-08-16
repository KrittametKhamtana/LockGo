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

    /// <summary>
    /// Grace window for a StartTime that's already slightly in the past. An
    /// "start now" booking carries the client's clock, so a few minutes of
    /// skew (or the user sitting on the confirm screen) must not be rejected.
    /// </summary>
    private static readonly TimeSpan StartTimePastGrace = TimeSpan.FromMinutes(5);

    /// <summary>How far ahead a compartment can be reserved.</summary>
    private static readonly TimeSpan MaxAdvanceBooking = TimeSpan.FromDays(30);

    /// <summary>
    /// How many times to re-pick a compartment after losing an optimistic-concurrency
    /// race. Each retry starts a fresh transaction and re-queries availability, so the
    /// only cost of exhausting these is falling back to a NO_AVAILABILITY conflict —
    /// which, under heavy contention on the last few compartments, is the honest answer.
    /// </summary>
    private const int MaxClaimAttempts = 5;

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

        if (!Enum.TryParse<CompartmentSize>(request.Size, ignoreCase: true, out var size))
        {
            throw new ValidationAppException($"Size must be one of: {string.Join(", ", Enum.GetNames<CompartmentSize>())}.");
        }

        var now = DateTimeOffset.UtcNow;

        if (request.StartTime < now - StartTimePastGrace)
        {
            throw new ValidationAppException("StartTime cannot be in the past.");
        }

        if (request.StartTime > now + MaxAdvanceBooking)
        {
            throw new ValidationAppException($"StartTime cannot be more than {MaxAdvanceBooking.TotalDays:0} days in the future.");
        }

        // Concurrent requests for the same size all pick the same first-free
        // compartment, so all but one lose the xmin race even when siblings are
        // still free. Retrying re-queries for the next free compartment instead
        // of reporting a false "no availability". Bounded so a genuinely full
        // locker still fails fast; MaxClaimAttempts covers realistic contention
        // without letting a request spin.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(ct2 => CreateInTransactionAsync(request, size, ct2), ct);
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
            catch (CompartmentClaimConflictException) when (attempt < MaxClaimAttempts)
            {
                // Lost the race for one specific compartment — loop and try the next.
            }
            catch (CompartmentClaimConflictException)
            {
                // Out of retries: sustained contention on the remaining compartments.
                // Report it as a normal availability conflict rather than leaking an
                // internal signal (which the handler would turn into a bare 500).
                throw new ConflictException("NO_AVAILABILITY", $"No {size} compartment is available at this locker right now.");
            }
        }
    }

    private async Task<ReservationDto> CreateInTransactionAsync(
        CreateReservationRequest request,
        CompartmentSize size,
        CancellationToken ct)
    {
        var existing = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
        if (existing is not null)
        {
            return MapToDto(existing);
        }

        var startTime = request.StartTime;
        var endTime = startTime.AddHours(request.DurationHours);

        // Picks a compartment of the requested size with no overlapping active
        // reservation — checked against live Reservation rows, never the
        // denormalized Status column. Returning null means every compartment of
        // that size is taken for this window.
        var compartment = await _compartmentRepository.FindAvailableAsync(request.LockerId, size, startTime, endTime, ct);

        if (compartment is null)
        {
            // Race note: a concurrent request holding the same idempotency key may
            // have taken the last compartment between our check above and here.
            // Re-check before reporting a conflict, so a double-click gets its own
            // booking back rather than a misleading "no availability".
            var wonByReplay = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            if (wonByReplay is not null)
            {
                return MapToDto(wonByReplay);
            }

            // Distinguish "fully booked" (409) from "this locker doesn't offer
            // that size at all" (404) — very different messages for the client.
            var sizeExists = await _compartmentRepository.ExistsForSizeAsync(request.LockerId, size, ct);

            throw sizeExists
                ? new ConflictException("NO_AVAILABILITY", $"No {size} compartment is available at this locker right now.")
                : new NotFoundException("COMPARTMENT_NOT_FOUND", $"Locker '{request.LockerId}' has no {size} compartment.");
        }

        if (compartment.Locker.OperatingStatus != OperatingStatus.Open)
        {
            throw new ConflictException("LOCKER_CLOSED", "This locker is currently closed.");
        }

        var reservation = new Reservation
        {
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

    public async Task<ReservationDto> GetByBookingNumberAsync(string bookingNumber, CancellationToken ct)
    {
        var reservation = await _reservationRepository.GetByBookingNumberAsync(bookingNumber, ct)
            ?? throw new NotFoundException("RESERVATION_NOT_FOUND", $"Reservation '{bookingNumber}' was not found.");

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
