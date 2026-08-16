using LockGo.Application.Common;
using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LockGo.Api.Controllers;

[ApiController]
[Route("api/lockers")]
public class LockersController : ControllerBase
{
    private readonly ILockerService _lockerService;

    public LockersController(ILockerService lockerService)
    {
        _lockerService = lockerService;
    }

    /// <summary>
    /// location: "lat,lng" (optional — omit to skip distance filtering/sorting).
    /// distance: max distance in km, only applied when location is supplied.
    /// size: S | M | L.
    /// availability: true to only return lockers with at least one available compartment.
    /// search: free-text match on locker name or address.
    /// startTime/durationHours: the slot to report availability for; omit for "right now".
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LockerListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LockerListItemDto>>> Search(
        [FromQuery] string? location,
        [FromQuery] double? distance,
        [FromQuery] string? size,
        [FromQuery] bool? availability,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? startTime,
        [FromQuery] int? durationHours,
        CancellationToken ct)
    {
        (double lat, double lng)? origin = TryParseLocation(location);

        var query = new LockerSearchQuery(
            origin?.lat,
            origin?.lng,
            distance,
            size,
            availability,
            search,
            startTime,
            durationHours);

        var result = await _lockerService.SearchAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<LockerDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LockerDetailDto>> GetById(
        int id,
        [FromQuery] DateTimeOffset? startTime,
        [FromQuery] int? durationHours,
        CancellationToken ct)
    {
        var locker = await _lockerService.GetByIdAsync(id, BookingWindow.From(startTime, durationHours), ct);
        return Ok(locker);
    }

    private static (double lat, double lng)? TryParseLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        var parts = location.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            double.TryParse(parts[0], System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
            double.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var lng))
        {
            return (lat, lng);
        }

        return null;
    }
}
