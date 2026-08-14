namespace LockGo.Application.DTOs;

public record ReservationDto(
    Guid Id,
    string BookingNumber,
    Guid LockerId,
    string LockerName,
    string LockerAddress,
    Guid CompartmentId,
    string CompartmentSize,
    decimal Price,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    bool IsActive);
