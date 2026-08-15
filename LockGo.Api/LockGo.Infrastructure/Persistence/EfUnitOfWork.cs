using LockGo.Application.Common.Exceptions;
using LockGo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LockGo.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private const string IdempotencyKeyUniqueIndexName = "ix_reservations_idempotency_key";
    private const string EmailUniqueIndexName = "ix_users_email";
    private const string UsernameUniqueIndexName = "ix_users_username";

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
                // idempotency keys) both picked this same compartment before either
                // committed. Postgres serializes their UPDATEs; the loser's xmin is stale,
                // EF reports 0 rows affected here. This is the actual mutual exclusion for
                // rule 2 (no double-booking) — the earlier availability read alone can't
                // guarantee it under READ COMMITTED.
                //
                // Deliberately NOT a NO_AVAILABILITY conflict: a sibling compartment of
                // the same size may still be free. ReservationService retries on this.
                await transaction.RollbackAsync(ct);
                throw new CompartmentClaimConflictException();
            }
            catch (DbUpdateException ex) when (IsIdempotencyKeyUniqueViolation(ex))
            {
                await transaction.RollbackAsync(ct);
                throw new IdempotencyKeyConflictException("(see inner exception for the offending key)");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex, EmailUniqueIndexName))
            {
                // Lost the race against another signup with the same email — the
                // pre-check in AuthService caught the common case, this is the backstop.
                await transaction.RollbackAsync(ct);
                throw new ConflictException("EMAIL_TAKEN", "An account with this email already exists.");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex, UsernameUniqueIndexName))
            {
                await transaction.RollbackAsync(ct);
                throw new ConflictException("USERNAME_TAKEN", "This username is already taken.");
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
    private static bool IsIdempotencyKeyUniqueViolation(DbUpdateException ex) =>
        IsUniqueViolation(ex, IdempotencyKeyUniqueIndexName);

    private static bool IsUniqueViolation(DbUpdateException ex, string constraintName)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx
               && pgEx.ConstraintName == constraintName;
    }
}
