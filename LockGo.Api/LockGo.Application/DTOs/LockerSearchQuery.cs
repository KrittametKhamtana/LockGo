namespace LockGo.Application.DTOs;

/// <summary>
/// Distance filtering/sorting is only applied when Lat/Lng are supplied —
/// there's no device geolocation or geocoding service in this scope, so the
/// caller passes coordinates directly (the frontend gets them from the
/// browser's Geolocation API when the user opts in).
/// Search is a free-text match on locker name/address, independent of Lat/Lng.
///
/// StartTime/DurationHours describe the slot the caller intends to book.
/// Availability is reported for that window, so a locker that's full now but
/// free tomorrow shows as free when tomorrow is what was asked for. Omitting
/// them means "right now".
/// </summary>
public record LockerSearchQuery(
    double? Lat,
    double? Lng,
    double? MaxDistanceKm,
    string? Size,
    bool? AvailableOnly,
    string? Search,
    DateTimeOffset? StartTime = null,
    int? DurationHours = null);
