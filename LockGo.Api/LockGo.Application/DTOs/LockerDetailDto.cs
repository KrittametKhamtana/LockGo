namespace LockGo.Application.DTOs;

public record LockerDetailDto(
    Guid Id,
    string Name,
    string Address,
    double Lat,
    double Lng,
    string OperatingStatus,
    IReadOnlyList<CompartmentDto> Compartments);
