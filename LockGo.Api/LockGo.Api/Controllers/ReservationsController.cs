using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LockGo.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    /// <summary>
    /// Idempotent on IdempotencyKey — safe to retry, including a rapid
    /// double-click on the Confirm button re-sending the same request.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ReservationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> Create([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var reservation = await _reservationService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByBookingNumber), new { bookingNumber = reservation.BookingNumber }, reservation);
    }

    /// <summary>
    /// Keyed on the public BookingNumber, not the sequential primary key —
    /// the confirmation URL is a shareable permalink, and an incrementing id
    /// there would let anyone walk it to read other people's bookings.
    /// </summary>
    [HttpGet("{bookingNumber}")]
    [ProducesResponseType<ReservationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> GetByBookingNumber(string bookingNumber, CancellationToken ct)
    {
        var reservation = await _reservationService.GetByBookingNumberAsync(bookingNumber, ct);
        return Ok(reservation);
    }
}
