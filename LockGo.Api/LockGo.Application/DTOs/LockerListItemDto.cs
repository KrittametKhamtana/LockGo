namespace LockGo.Application.DTOs;

public record LockerListItemDto(
    int Id,
    string Name,
    string Address,
    double Lat,
    double Lng,
    string OperatingStatus,
    double? DistanceKm,
    decimal MinPrice,
    int AvailableCompartmentCount,
    /// <summary>Per-size breakdown so the list can show "S: 2 left · M: none · L: 1 left".</summary>
    IReadOnlyList<CompartmentSizeAvailabilityDto> SizeAvailability,
    /// <summary>
    /// True when the locker is Open but every compartment is taken. Distinct
    /// from OperatingStatus == Closed, which means the site itself isn't
    /// operating — this one is "open, but nothing free right now".
    /// </summary>
    bool IsFullyBooked);
