using LockGo.Domain.Entities;

namespace LockGo.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    Task<bool> UsernameExistsAsync(string username, CancellationToken ct);

    Task<User?> GetByUsernameAsync(string username, CancellationToken ct);

    void Add(User user);
}
