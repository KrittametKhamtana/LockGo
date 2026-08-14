namespace LockGo.Application.DTOs;

public record LockerListItemDto(
    Guid Id,
    string Name,
    string Address,
    double Lat,
    double Lng,
    string OperatingStatus,
    double? DistanceKm,
    decimal MinPrice,
    int AvailableCompartmentCount);
