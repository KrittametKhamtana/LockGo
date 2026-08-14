using System.Threading;
using LockGo.Application.Common.Exceptions;
using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;

namespace LockGo.Tests.Concurrency;

/// <summary>
/// A hand-written fake (not a Moq mock) that mimics the two things Postgres
/// actually guarantees on the booking write path:
///   1. the unique index on idempotency_key — two concurrent inserts for the
///      same key can't both succeed (throws IdempotencyKeyConflictException,
///      matching what EfUnitOfWork translates SQLSTATE 23505 into);
///   2. xmin optimistic concurrency on Compartment — two different requests
///      can't both claim the same compartment for an overlapping window
///      (throws ConflictException/NO_AVAILABILITY, matching what
///      EfUnitOfWork translates DbUpdateConcurrencyException into).
/// Both checks live inside the same lock as the insert, which is the fake's
/// stand-in for the database serializing those writes.
///
/// The tricky part of faking this: the reads here complete synchronously
/// (Task.FromResult under the hood), so awaiting them never yields the thread
/// — without help, Task.WhenAll's logical "requests" would just run back-to-back
/// on one thread and never actually race. expectedConcurrentCallers wires a
/// Barrier into the FIRST wave of GetByIdempotencyKeyAsync calls so every
/// concurrent caller finishes its "does this key exist yet?" check before any
/// of them is allowed to proceed — that's what turns the check-then-insert
/// race into something reproduced deterministically instead of hoping the
/// scheduler happens to interleave two near-instant fake calls. Later calls
/// (the recovery re-read after a conflict) aren't part of that wave and skip
/// the barrier entirely.
/// </summary>
public class RacyReservationRepository : IReservationRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Reservation> _byIdempotencyKey = new();
    private readonly List<Reservation> _all = new();
    private readonly Barrier? _checkBarrier;
    private readonly int _expectedConcurrentCallers;
    private int _checkCallCount;

    public RacyReservationRepository(int expectedConcurrentCallers = 1)
    {
        _expectedConcurrentCallers = expectedConcurrentCallers;
        _checkBarrier = expectedConcurrentCallers > 1 ? new Barrier(expectedConcurrentCallers) : null;
    }

    public int InsertedCount { get; private set; }

    public async Task<Reservation?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct)
    {
        var callIndex = Interlocked.Increment(ref _checkCallCount);

        Reservation? existing;
        lock (_gate)
        {
            _byIdempotencyKey.TryGetValue(idempotencyKey, out existing);
        }

        if (_checkBarrier is not null && callIndex <= _expectedConcurrentCallers)
        {
            await Task.Run(() => _checkBarrier.SignalAndWait(ct), ct);
        }

        return existing;
    }

    public Task<Reservation?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        lock (_gate)
        {
            return Task.FromResult(_all.FirstOrDefault(r => r.Id == id));
        }
    }

    /// <summary>
    /// Synchronous counterpart to the real repository's SQL overlap predicate —
    /// StubCompartmentRepository calls this to decide which compartments are
    /// still free, so the two fakes agree on availability the way the real
    /// query and DB do.
    /// </summary>
    public bool HasOverlap(Guid compartmentId, DateTimeOffset start, DateTimeOffset end)
    {
        lock (_gate)
        {
            return _all.Any(r =>
                r.CompartmentId == compartmentId &&
                r.Status == ReservationStatus.Active &&
                r.StartTime < end &&
                r.EndTime > start);
        }
    }

    public void Add(Reservation reservation)
    {
        lock (_gate)
        {
            if (_byIdempotencyKey.ContainsKey(reservation.IdempotencyKey))
            {
                throw new IdempotencyKeyConflictException(reservation.IdempotencyKey);
            }

            // Stand-in for xmin optimistic concurrency: a caller that picked this
            // compartment before a concurrent caller committed to it loses here,
            // exactly as its stale xmin would make the UPDATE affect 0 rows.
            var alreadyClaimed = _all.Any(r =>
                r.CompartmentId == reservation.CompartmentId &&
                r.Status == ReservationStatus.Active &&
                r.StartTime < reservation.EndTime &&
                r.EndTime > reservation.StartTime);

            if (alreadyClaimed)
            {
                throw new CompartmentClaimConflictException();
            }

            _byIdempotencyKey[reservation.IdempotencyKey] = reservation;
            _all.Add(reservation);
            InsertedCount++;
        }
    }
}
