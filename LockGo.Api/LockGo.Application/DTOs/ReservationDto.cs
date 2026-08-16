namespace LockGo.Application.DTOs;

public record ReservationDto(
    int Id,
    string BookingNumber,
    int LockerId,
    string LockerName,
    string LockerAddress,
    int CompartmentId,
    string CompartmentSize,
    decimal Price,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    bool IsActive);
