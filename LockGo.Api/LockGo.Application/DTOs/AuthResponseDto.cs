namespace LockGo.Application.DTOs;

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Username);
