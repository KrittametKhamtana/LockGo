using LockGo.Application.Common.Exceptions;
using LockGo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LockGo.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private const string IdempotencyKeyUniqueIndexName = "ix_reservations_idempotency_key";

    private readonly LockGoDbContext _db;

    public EfUnitOfWork(LockGoDbContext db)
    {
        _db = db;
    }

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        // The DbContext is configured with EnableRetryOnFailure (transient-error
        // resilience against a pooled connection to a hosted Postgres instance).
        // That execution strategy forbids a manually-started transaction unless the
        // whole "begin/operation/commit" unit is run through it — otherwise EF
        // throws InvalidOperationException on every call. Wrapping it here also
        // means a genuinely transient failure retries the entire attempt, which is
        // safe precisely because CreateInTransactionAsync is idempotent (it re-checks
        // the idempotency key before inserting).
        var strategy = _db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                var result = await operation(ct);
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Two genuinely different requests (not a double-click replay — different
                // idempotency keys) both passed the overlap check before either committed.
                // Postgres serializes their compartment-status UPDATEs; the loser's xmin
                // is stale, EF reports 0 rows affected here. This is the actual mutual
                // exclusion for rule 2 (no double-booking) — the earlier overlap read
                // alone can't guarantee it under READ COMMITTED.
                await transaction.RollbackAsync(ct);
                throw new ConflictException("NO_AVAILABILITY", "This compartment was just booked by another request.");
            }
            catch (DbUpdateException ex) when (IsIdempotencyKeyUniqueViolation(ex))
            {
                await transaction.RollbackAsync(ct);
                throw new IdempotencyKeyConflictException("(see inner exception for the offending key)");
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    /// <summary>
    /// Final safety net behind the in-transaction idempotency check: two
    /// requests that both passed the "does this key already exist" read can
    /// still race to insert. Postgres' unique constraint rejects the loser
    /// with SQLSTATE 23505, which we translate here instead of leaking a raw
    /// DbUpdateException up to the API layer.
    /// </summary>
    private static bool IsIdempotencyKeyUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx
               && pgEx.ConstraintName == IdempotencyKeyUniqueIndexName;
    }
}
