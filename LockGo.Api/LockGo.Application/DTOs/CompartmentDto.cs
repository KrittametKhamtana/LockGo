namespace LockGo.Application.DTOs;

public record CompartmentDto(
    Guid Id,
    string Size,
    decimal Price,
    string Status);
