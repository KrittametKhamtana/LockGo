using LockGo.Application.DTOs;

namespace LockGo.Application.Interfaces;

public interface ILockerService
{
    Task<IReadOnlyList<LockerListItemDto>> SearchAsync(LockerSearchQuery query, CancellationToken ct);

    /// <summary>Throws NotFoundException if the locker doesn't exist.</summary>
    Task<LockerDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
}
