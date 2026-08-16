namespace LockGo.Application.DTOs;

public record LockerDetailDto(
    int Id,
    string Name,
    string Address,
    double Lat,
    double Lng,
    string OperatingStatus,
    /// <summary>
    /// Grouped by size rather than a flat compartment list — the booking flow
    /// picks a size and the server assigns a free compartment, so exposing
    /// individual compartment IDs to the client would be misleading.
    /// </summary>
    IReadOnlyList<CompartmentSizeAvailabilityDto> SizeAvailability,
    bool IsFullyBooked);
