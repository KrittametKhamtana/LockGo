# Testing

## Summary

**60 backend tests, all passing** (verified with `dotnet test`), across
three kinds:

| Kind | Location | What it covers |
|---|---|---|
| Unit | `LockGo.Tests/Unit/` | `ReservationService`, `LockerService`, `AuthService` business rules, against hand-written fakes/mocks — no database |
| Concurrency | `LockGo.Tests/Concurrency/` | The double-click / race-condition guarantees specifically — a hand-rolled fake with real thread synchronization, not a mock |
| Integration | `LockGo.Tests/Integration/` | HTTP-level, via `WebApplicationFactory` (in-memory test server + EF Core InMemory provider) — real routing, model binding, middleware |

No frontend automated test runner is configured (no Vitest/Jest) — `npm
run lint` and `npm run build`'s type-check (`tsc -b`) are what CI enforces
on the frontend. Feature-level frontend verification was done by driving
the running app in a real browser instead — see
[`AI_USAGE.md`](AI_USAGE.md) and the live-database section below.

## Running the tests

```bash
cd LockGo.Api
dotnet test
```

```bash
cd frontend
npm run lint
npm run build   # also type-checks
```

Both are run in CI (`.github/workflows/ci.yml`) on every push/PR to
`develop`/`master`.

## What's covered, by class

### Unit — `ReservationServiceTests.cs`

Reserve success, no-availability (409), size the locker doesn't offer at
all (404), locker closed (409), duration out of range, start time in the
past vs. within clock-skew grace, start time too far in the future,
invalid size string, and — the core guarantee — replaying the same
`idempotencyKey` returns the original reservation instead of creating a
second one.

### Unit — `LockerServiceTests.cs`

Per-size availability reporting (including sizes the locker doesn't
offer), "fully booked but still Open" vs. "Closed", expired reservations
correctly excluded from availability, and the size-filtered search
regression (`SearchAsync_WhenFilteredBySize_...`) that reproduces the real
bug described below.

### Unit — `AuthServiceTests.cs`

Sign-up success, password hashed (never stored/returned in plain text),
duplicate email/duplicate username both rejected, password-mismatch and
too-short-password validation, invalid-email validation, sign-in success,
and wrong-password vs. unknown-username both mapped to the same 401 (not
distinguishable, so the error can't enumerate valid usernames).

### Concurrency — `DoubleClickConfirmTests.cs`

The one suite with an explicit correctness requirement beyond "returns the
right JSON":

- Many concurrent calls with the **same** `idempotencyKey` → exactly one
  reservation is created (proves the double-click guarantee, not just the
  happy path).
- Concurrent calls with **different** `idempotencyKey`s for the same size
  → each successfully claims a **separate** compartment, and once
  compartments run out, the excess callers get a real `409`, not a false
  one.

Needs a hand-rolled fake (`RacyReservationRepository`, `Barrier`-based)
rather than a mock, because naive `Task.WhenAll` over a
synchronously-resolving fake never actually races — the first version of
this test passed for the wrong reason. Full story in
[`DEBUGGING.md`](DEBUGGING.md).

### Integration — `LockersApiTests.cs`, `ReservationsApiTests.cs`, `AuthApiTests.cs`

Same scenarios as the unit tests, but exercised through real HTTP
routing/model binding/middleware via `WebApplicationFactory`, including
the full request → response error-shape contract (`{error:{code,message}}`)
for each documented status code.

## Verified against a live database

The full flow (search → detail → reserve → confirm, including genuinely
concurrent double-click requests) has also been run against a real hosted
Postgres instance (Neon), not just the test suite's InMemory provider.
That pass caught two real bugs every automated test had missed:

1. `POST /api/reservations` returned `500` on every call — `EnableRetryOnFailure`
   conflicts with a manually-started transaction unless the whole unit runs
   through EF's execution strategy; the InMemory provider doesn't implement
   retrying execution strategies, so no test exercised this path.
2. `size` + `availability` filters combined incorrectly — a locker with an
   occupied Small but an available Medium passed both filters
   independently. The seed data (every locker starts fully available)
   couldn't reveal this until a real booking changed one compartment's
   state on the live database.

Both are now fixed and covered by dedicated tests
(`GetLockers_FilteredBySizeAndAvailability_ExcludesLockerWhoseOnlyAvailableCompartmentIsADifferentSize`,
`SearchAsync_WhenFilteredBySize_ReportsHeadlinePriceAndCountForThatSizeOnly`).
Full story: [`AI_USAGE.md`](AI_USAGE.md#4-live-postgres-verification--and-two-real-bugs-it-caught).

**Not yet done:** `EXPLAIN ANALYZE` against a realistic data volume (seed
data is only a handful of rows per locker); a full click-through of all
four frontend screens against live data (only the FindLocker page's
loading/error states were checked structurally).
