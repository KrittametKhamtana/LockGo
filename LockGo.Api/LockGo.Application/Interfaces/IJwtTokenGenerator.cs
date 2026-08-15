using LockGo.Domain.Entities;

namespace LockGo.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
