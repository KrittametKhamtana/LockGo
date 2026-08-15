using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LockGo.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LockGoDbContext _db;

    public UserRepository(LockGoDbContext db)
    {
        _db = db;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _db.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct) =>
        _db.Users.AsNoTracking().AnyAsync(u => u.Username == username, ct);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public void Add(User user)
    {
        _db.Users.Add(user);
    }
}
