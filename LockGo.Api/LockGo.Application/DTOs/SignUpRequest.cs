namespace LockGo.Application.DTOs;

public record SignUpRequest(
    string FirstName,
    string LastName,
    string Email,
    string Username,
    string Password,
    string ConfirmPassword);
