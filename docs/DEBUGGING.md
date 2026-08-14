# Debugging challenge: double-click Confirm must not duplicate a reservation

This isn't a manufactured example — it's the actual bug found while writing
[`LockGo.Tests/Concurrency/DoubleClickConfirmTests.cs`](../LockGo.Api/LockGo.Tests/Concurrency/DoubleClickConfirmTests.cs),
left in as the debugging write-up because it's a more honest artifact than a
staged one.

## Symptom

The concurrency test fires two `ReservationService.CreateAsync` calls
**genuinely concurrently** (`Task.WhenAll`, not two sequential awaits) with
the same `idempotencyKey` — simulating a double-clicked Confirm button — and
asserts exactly one reservation gets created and both calls return it.

First version of the fix (catching the DB's unique-constraint violation on
`idempotency_key` and re-reading the winning row) looked correct by
inspection and the test passed. But the test was passing *for the wrong
reason*: the fake repository's methods resolved synchronously
(`Task.FromResult`), so `await`ing them never actually yielded the thread —
`Task.WhenAll`'s two "concurrent" calls were in practice running back to
back on one thread, never truly racing. Disabling the fix entirely (via a
`when (false)` exception filter) still made the test pass, which is the
tell that a concurrency test isn't testing concurrency.

## Root cause

Two separate things, found in that order:

**1. The test wasn't racing.** Fixed by adding a `Barrier` to the fake
repository (`RacyReservationRepository`) that forces every concurrent caller
to finish its "does this idempotency key already exist?" check before any
of them is allowed to proceed to `Add()` — turning the check-then-insert
race from something the scheduler *might* interleave into something it
*always* does. See the class doc comment in
[`RacyReservationRepository.cs`](../LockGo.Api/LockGo.Tests/Concurrency/RacyReservationRepository.cs)
for the full mechanics.

**2. With the race now actually happening, a real bug surfaced in
`ReservationService`.** Sequence, under Postgres `READ COMMITTED`:

1. Request A and Request B both carry the same `idempotencyKey` (a
   double-click). Both check "does a reservation with this key exist?" —
   neither sees one yet, since neither has committed.
2. Request A proceeds: no overlap found, inserts its reservation, commits.
3. Request B's *own* overlap check — a separate `SELECT`, re-evaluated
   fresh under `READ COMMITTED` — now runs **after** A's commit. It finds
   A's just-committed reservation on the same compartment/time window and
   concludes the compartment is unavailable.
4. Request B throws `ConflictException("NO_AVAILABILITY", ...)`.

The bug: the "overlapping reservation" B found *was A's copy of the same
logical booking*, not a competing one — but the code had no way to tell the
difference, because the overlap query doesn't look at `idempotency_key` at
all. The user who double-clicked would see one browser tab succeed and the
other show a confusing "not available" error, for a compartment they'd
literally just been given.

## Where it's fixed

[`ReservationService.CreateInTransactionAsync`](../LockGo.Api/LockGo.Application/Services/ReservationService.cs) —
when the overlap check comes back positive, re-check the idempotency key
once more before concluding it's a real conflict:

```csharp
if (hasOverlap)
{
    var wonByReplay = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
    if (wonByReplay is not null)
    {
        return MapToDto(wonByReplay);
    }

    throw new ConflictException("NO_AVAILABILITY", "This compartment is not available for the selected time.");
}
```

## How to verify

```bash
cd LockGo.Api
dotnet test --filter "FullyQualifiedName~Concurrency"
```

Both concurrency tests exercise this: `CreateAsync_CalledConcurrentlyWithSameIdempotencyKey_CreatesExactlyOneReservation`
and the 20-caller variant. A matching unit test,
`CreateAsync_WhenOverlapIsActuallyOwnReplayWonByAConcurrentRequest_ReturnsThatReservationInsteadOfConflict`
in `ReservationServiceTests.cs`, reproduces the exact sequence above with
mocks (first idempotency check returns null, overlap check returns true,
second idempotency check returns the winning reservation) without needing
real concurrency at all.

To see it fail again: revert just the four added lines in
`CreateInTransactionAsync` (go straight from `hasOverlap` to `throw`) and
re-run the concurrency tests.

## How to prevent recurrence

- **A concurrency test that doesn't force a real race proves nothing.**
  Prefer `Task.WhenAll` over sequential awaits when the property under test
  is "what happens when two things happen at once" — and if the test double
  resolves synchronously, add an explicit synchronization point (a
  `Barrier`, a `TaskCompletionSource`) rather than trusting the scheduler to
  interleave two near-instant fake calls.
- Any code path that decides "is this a conflict?" using data that doesn't
  distinguish *my own retry* from *someone else's write* is suspect —
  overlap/availability checks should be idempotency-aware, or the write
  path should re-verify idempotency immediately before surfacing a
  conflict, as done here.
- This only reproduces under genuine `READ COMMITTED` interleaving (per-
  statement snapshots), which the EF Core `InMemory` provider can't model —
  the concurrency test uses a hand-rolled fake specifically because it can
  enforce that interleaving deterministically. Don't rely on the
  `LockersApiTests`/`ReservationsApiTests` integration tests (InMemory-
  backed) to catch races like this one.
