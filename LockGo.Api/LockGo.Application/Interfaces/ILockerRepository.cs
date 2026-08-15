using LockGo.Application.Common;
using LockGo.Application.DTOs;
using LockGo.Domain.Entities;

namespace LockGo.Application.Interfaces;

public interface ILockerRepository
{
    Task<IReadOnlyList<Locker>> SearchAsync(LockerSearchQuery query, CancellationToken ct);
    Task<Locker?> GetByIdAsync(Guid id, BookingWindow window, CancellationToken ct);
}
