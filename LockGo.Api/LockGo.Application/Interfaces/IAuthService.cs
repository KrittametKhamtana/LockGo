using LockGo.Application.DTOs;

namespace LockGo.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> SignUpAsync(SignUpRequest request, CancellationToken ct);

    Task<AuthResponseDto> SignInAsync(SignInRequest request, CancellationToken ct);
}
