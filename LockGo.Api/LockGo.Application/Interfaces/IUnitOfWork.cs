namespace LockGo.Application.Interfaces;

/// <summary>
/// Wraps one DB transaction around an operation and commits/rolls back
/// around it, without leaking EF Core types into the Application layer.
/// </summary>
public interface IUnitOfWork
{
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);
}
