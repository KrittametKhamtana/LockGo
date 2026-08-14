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
        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, reservation);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ReservationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> GetById(Guid id, CancellationToken ct)
    {
        var reservation = await _reservationService.GetByIdAsync(id, ct);
        return Ok(reservation);
    }
}
