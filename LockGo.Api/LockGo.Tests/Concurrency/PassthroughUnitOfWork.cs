using LockGo.Application.Interfaces;

namespace LockGo.Tests.Concurrency;

/// <summary>Runs the operation with no real transaction — RacyReservationRepository.Add() is the piece under test.</summary>
public class PassthroughUnitOfWork : IUnitOfWork
{
    public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken ct = default)
        => operation(ct);
}
