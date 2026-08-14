using System.Threading;
using LockGo.Application.Common.Exceptions;
using LockGo.Application.Interfaces;
using LockGo.Domain.Entities;
using LockGo.Domain.Enums;

namespace LockGo.Tests.Concurrency;

/// <summary>
/// A hand-written fake (not a Moq mock) that mimics the one thing Postgres'
/// unique index on idempotency_key actually guarantees: two concurrent
/// inserts for the same key can't both succeed.
///
/// The tricky part of faking this: GetByIdempotencyKeyAsync/HasOverlapAsync
/// here complete synchronously (Task.FromResult under the hood), so awaiting
/// them never yields the thread — without help, Task.WhenAll's two logical
/// "requests" would just run back-to-back on one thread and never actually
/// race. expectedConcurrentCallers wires a Barrier into the FIRST wave of
/// GetByIdempotencyKeyAsync calls so every concurrent caller is forced to
/// finish its "does this key exist yet?" check before any of them is
/// allowed to proceed to Add() — that's what turns the check-then-insert
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

    public Task<bool> HasOverlapAsync(Guid compartmentId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        lock (_gate)
        {
            var overlap = _all.Any(r =>
                r.CompartmentId == compartmentId &&
                r.Status == ReservationStatus.Active &&
                r.StartTime < end &&
                r.EndTime > start);
            return Task.FromResult(overlap);
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

            _byIdempotencyKey[reservation.IdempotencyKey] = reservation;
            _all.Add(reservation);
            InsertedCount++;
        }
    }
}
