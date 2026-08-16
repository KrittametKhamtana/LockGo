namespace LockGo.Application.DTOs;

public record AuthResponseDto(
    string Token,
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Username);
