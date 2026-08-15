using System.Text.RegularExpressions;
using LockGo.Application.Common;
using LockGo.Application.Common.Exceptions;
using LockGo.Application.DTOs;
using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;

namespace LockGo.Application.Services;

public partial class AuthService : IAuthService
{
    private const int MinPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> SignUpAsync(SignUpRequest request, CancellationToken ct)
    {
        Validate(request);

        // Fast, friendly path — the unique index (see EfUnitOfWork) is the
        // authoritative backstop for the rare concurrent-signup race.
        if (await _userRepository.EmailExistsAsync(request.Email, ct))
        {
            throw new ConflictException("EMAIL_TAKEN", "An account with this email already exists.");
        }

        if (await _userRepository.UsernameExistsAsync(request.Username, ct))
        {
            throw new ConflictException("USERNAME_TAKEN", "This username is already taken.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = $"{request.FirstName} {request.LastName}",
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Username = request.Username,
            PasswordHash = PasswordHasher.Hash(request.Password),
        };

        await _unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            _userRepository.Add(user);
            return Task.FromResult(true);
        }, ct);

        return MapToDto(user);
    }

    public async Task<AuthResponseDto> SignInAsync(SignInRequest request, CancellationToken ct)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, ct);

        // Deliberately the same error for "no such user" and "wrong password" —
        // telling them apart would let an attacker enumerate valid usernames.
        if (user?.PasswordHash is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        return MapToDto(user);
    }

    private static void Validate(SignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationAppException("First name and last name are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ValidationAppException("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailPattern().IsMatch(request.Email))
        {
            throw new ValidationAppException("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinPasswordLength)
        {
            throw new ValidationAppException($"Password must be at least {MinPasswordLength} characters.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            throw new ValidationAppException("Passwords do not match.");
        }
    }

    private AuthResponseDto MapToDto(User user) => new(
        _jwtTokenGenerator.GenerateToken(user),
        user.Id,
        user.FirstName ?? string.Empty,
        user.LastName ?? string.Empty,
        user.Email ?? string.Empty,
        user.Username ?? string.Empty);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
