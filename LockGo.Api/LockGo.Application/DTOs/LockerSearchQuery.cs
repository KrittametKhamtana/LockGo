namespace LockGo.Application.DTOs;

/// <summary>
/// Distance filtering/sorting is only applied when Lat/Lng are supplied —
/// there's no device geolocation or geocoding service in this scope, so the
/// caller passes coordinates directly (e.g. parsed from a "lat,lng" location
/// string) rather than a free-text place name.
/// </summary>
public record LockerSearchQuery(
    double? Lat,
    double? Lng,
    double? MaxDistanceKm,
    string? Size,
    bool? AvailableOnly);
