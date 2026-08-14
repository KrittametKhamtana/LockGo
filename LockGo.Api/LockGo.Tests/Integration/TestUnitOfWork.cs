using LockGo.Application.Interfaces;
using LockGo.Infrastructure.Persistence;

namespace LockGo.Tests.Integration;

/// <summary>
/// Mirrors EfUnitOfWork minus the BeginTransaction/Commit/Rollback wrapping —
/// the InMemory provider used by these HTTP-level tests doesn't support real
/// transactions. SaveChanges still runs, so writes performed by the operation
/// are actually persisted.
/// </summary>
public class TestUnitOfWork : IUnitOfWork
{
    private readonly LockGoDbContext _db;

    public TestUnitOfWork(LockGoDbContext db)
    {
        _db = db;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        var result = await operation(ct);
        await _db.SaveChangesAsync(ct);
        return result;
    }
}
