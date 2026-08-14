namespace LockGo.Application.DTOs;

/// <summary>
/// Per-size rollup for one locker: how many compartments of this size exist
/// and how many are free right now. A locker can hold several compartments of
/// the same size, so the UI books a *size* rather than a specific compartment.
/// </summary>
public record CompartmentSizeAvailabilityDto(
    string Size,
    decimal Price,
    int AvailableCount,
    int TotalCount);
