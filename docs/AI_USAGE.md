# AI Usage

This project was built end-to-end in a single agentic session with Claude
Code (Claude Sonnet 5), working from a written project spec rather than
turn-by-turn chat. That's worth being upfront about, since it changes what
"prompts used" honestly looks like here — there weren't a dozen manual
back-and-forths; there was a detailed brief and one continuous
implementation pass, self-directed by the AI through the sub-goals below,
with the human reviewing the result rather than steering every step.

## 1. Prompts used

### Prompt 1 — the brief

> *(project spec attached — [`LockGo-project-spec.md`](../../LockGo-project-spec.md
> equivalent, reproduced as this repo's requirements)) followed by:* **"มี
> context มาให้นายทำ อ่านจากไฟล์ นี้เลย"** ("I've got context for you to work
> from — read it from this file.")

This is the actual instruction that kicked off implementation. It functions
as the master prompt: tech stack, screens, API shape, data model, business
rules 1–4, the hybrid availability model, error handling table, testing
requirements, and the deliverables checklist were all specified up front.
Everything else below is the AI decomposing that brief into buildable
pieces and executing them — not additional human prompts.

### Prompt 2 (self-directed) — "scaffold the backend to match the spec's suggested structure, using the actual latest tooling"

Interpreted "latest LTS" as .NET 10 (not preinstalled — required installing
the SDK), and "latest versions" for the frontend as whatever `npm create
vite@latest` + `npm install @mui/material@latest ...` actually resolves to.
This surfaced a real problem worth explaining rather than hiding: MUI
9.3.1's type definitions broke `Stack`/`Typography` prop typing under
TypeScript 6 in a way that made the project fail to build. The fix was
pinning `@mui/material`/`@mui/icons-material` to `7.3.11` (the latest
*stable major*, well past its `9.0.0-alpha` growing pains) rather than
chasing the newest tag — "latest" and "latest that actually type-checks"
aren't always the same version, and a project that doesn't build isn't a
deliverable.

### Prompt 3 (self-directed) — "implement business rule 4 (no duplicate reservations from double-click) and prove it with a test that would fail without the fix"

This is the one place the spec asked for more than "write code that looks
right" — section 10 explicitly asks for a test that *proves* the
double-click guarantee. Writing that test properly (see
[`docs/DEBUGGING.md`](DEBUGGING.md)) surfaced a real bug: the first version
of the fix passed a concurrency test that wasn't actually concurrent, and
once the test was corrected to force a genuine race, it caught a case where
a double-click's second request could get a false `409` instead of the
original booking. The fix and the story are documented there because it's
real, not staged.

### Prompt 4 (self-directed) — "the app needs to actually run — verify it in a browser, not just via `dotnet build`"

Per the project's own instructions, UI changes get checked in a real
browser before being called done. This caught the `networkMode`/focus-pause
red herring described in `ARCHITECTURE.md`'s testing notes (an artifact of
the headless preview tool, not a bug) and confirmed the FindLocker page's
error state actually renders the way the code implies it should — something
`dotnet build`/`npm run build` passing would never have caught.

## 2. AI workflow — what the AI did vs. what was human-decided

**Human-decided (in the spec):** the feature scope and user flow; the exact
tech stack (React/Vite/TS/MUI, .NET 10, PostgreSQL); the data model and its
field names; all four business rules, including the specific mechanism
mandated for rule 4 (idempotency key + optimistic concurrency, not
pessimistic locking); the hybrid availability model; the error-shape
contract; the testing bar ("prove double-click doesn't duplicate"); the
deliverables checklist itself.

**AI-decided (this session):** the concrete Clean-Architecture layering
(`Domain`/`Application`/`Infrastructure`/`Api`) and the repository +
`IUnitOfWork` abstractions used to keep `Application` free of EF Core so
its business rules are unit-testable; the specific EF Core mechanics for
mapping `xmin` (the spec named the *concept*, not the current provider API
— `UseXminAsConcurrencyToken()` was removed upstream, so a shadow
`Property<uint>("xmin").IsRowVersion()` was used instead); translating
Postgres' unique-constraint and concurrency-token violations into the
app's HTTP error shape in `EfUnitOfWork`; the entire test suite's design,
including the hand-rolled `RacyReservationRepository` fake and its
`Barrier`-based synchronization (needed because naive `Task.WhenAll` over
synchronously-resolving fakes doesn't actually race, as discovered in
Prompt 3 above); every line of application/UI code; dependency version
choices (MUI 7 over 9, TypeScript kept at latest, `dotnet-ef` tool version
bumped to match); and this documentation set.

## 3. AI-generated code review

Reviewing [`ReservationService.CreateInTransactionAsync` +
`EfUnitOfWork.ExecuteInTransactionAsync`](../LockGo.Api/LockGo.Application/Services/ReservationService.cs) —
the highest-stakes section in the codebase, since it's the only code with a
correctness requirement beyond "returns the right JSON."

| Aspect | Finding | Fix applied |
|---|---|---|
| **Correctness** | Overlap check could return a false conflict for a request racing its own idempotency-key replay (see `docs/DEBUGGING.md`). | Re-check `idempotencyKey` before throwing `NO_AVAILABILITY`; return the winning replay instead. |
| **Correctness** | `DbUpdateConcurrencyException` (from a genuine two-different-requests race on the same compartment) is a subtype of `DbUpdateException`; if the idempotency-key-unique-violation catch clause were written first without a type-specific guard, it could accidentally swallow a concurrency exception meant for a different code path. | `EfUnitOfWork` catches `DbUpdateConcurrencyException` in its own clause, ordered before the more general `DbUpdateException` filter, so each failure mode maps to the right error code. |
| **Security** | `BookingNumberGenerator` originally reached for `System.Random`-style generation. Booking numbers are shown to users but aren't used as secrets or lookup keys anywhere sensitive, so predictability isn't a real vulnerability here — but there's no cost to doing it right. | Uses `RandomNumberGenerator.GetInt32` (cryptographically strong) instead of a non-cryptographic PRNG, and excludes visually-ambiguous characters (`0`/`O`, `1`/`I`). |
| **Performance** | The overlap query filters `CompartmentId + Status + StartTime + EndTime` — without a matching index this is a sequential scan on every booking attempt, which matters on a CPU-limited free-tier server. | Composite index `(compartment_id, status, start_time, end_time)` added in `ReservationConfiguration`. Confirmed against the live Neon instance that queries and reservation writes succeed; `EXPLAIN ANALYZE` against a realistic data volume (this seed data is only a handful of rows) still hasn't been run and is worth doing before real load. |
| **Maintainability** | `IdempotencyKeyConflictException` is an internal signal (not a subtype of the public `AppException` hierarchy) that should never reach the API layer unhandled. | Documented explicitly in its own doc comment; if it ever *does* leak, the exception handler's fallback (bare `500 INTERNAL_ERROR`, logged) makes that visible rather than silently mismapping it to a wrong HTTP status. |
| **Maintainability** | `ReservationService` depends on `IUnitOfWork`, `IReservationRepository`, `ICompartmentRepository` — none of which reference EF Core — so the whole class can be unit-tested with hand-written fakes/Moq without a database. Verified this actually holds by writing `ReservationServiceTests.cs` against mocks and confirming zero references to `Microsoft.EntityFrameworkCore` crept into `LockGo.Application.csproj`. | No fix needed — flagged as a thing to *keep* true as the codebase grows, not a defect. |

## 4. Live Postgres verification — and two real bugs it caught

Real credentials for a hosted (Neon) Postgres instance became available
after the initial build. Running the actual API against it immediately
surfaced two bugs that every automated test in the suite — including the
InMemory-backed integration tests — had missed:

1. **`POST /api/reservations` returned 500 on every call.**
   `EfUnitOfWork` starts a transaction manually
   (`Database.BeginTransactionAsync`), but the DbContext is also configured
   with `EnableRetryOnFailure` for resilience against a pooled connection.
   EF Core forbids combining the two unless the whole unit runs through
   `Database.CreateExecutionStrategy()` — otherwise it throws
   `InvalidOperationException` before touching the database at all. The
   EF Core `InMemory` provider used by `LockGoWebApplicationFactory` doesn't
   implement retrying execution strategies, so this code path was simply
   never exercised by `ReservationsApiTests` despite them covering the same
   endpoint. **Fixed** by wrapping the transaction in
   `CreateExecutionStrategy().ExecuteAsync(...)` — safe to retry as a whole
   unit specifically because `CreateInTransactionAsync` is already
   idempotent (see `docs/DEBUGGING.md`).

2. **`size` + `availability` combined incorrectly.** `LockerRepository.SearchAsync`
   checked `Compartments.Any(c => c.Size == size)` and
   `Compartments.Any(c => c.Status == Available)` as two independent
   filters. A locker with an *occupied* Small compartment but an
   *available* Medium one passed both checks separately, even though it had
   no available compartment matching the requested size — `GET
   /api/lockers?size=S&availability=true` would still return it. This is
   exactly what the project spec's seed data couldn't reveal (every seeded
   locker starts with all three sizes available, so no combination of
   filters excludes anything until a booking changes one compartment's
   status) — it only showed up once a real reservation had been made
   against the live database. **Fixed** by combining both conditions into
   one `Any()` predicate over the same compartment; `LockerService` was
   also updated so a size-filtered search's `minPrice`/
   `availableCompartmentCount` reflect only that size instead of the
   locker's other compartments.

Both are covered by new tests —
`GetLockers_FilteredBySizeAndAvailability_ExcludesLockerWhoseOnlyAvailableCompartmentIsADifferentSize`
in `LockersApiTests.cs` (which manipulates compartment state directly via
the DbContext to reproduce the exact scenario) and
`SearchAsync_WhenFilteredBySize_ReportsPriceAndAvailabilityForThatSizeOnly`
in `LockerServiceTests.cs` — and both were re-verified against the live
Neon instance afterward, including firing two genuinely concurrent
`POST /api/reservations` requests with the same idempotency key at the real
database and confirming they returned the identical reservation.

**Takeaway kept for the record:** this is the second time in this project
that a concurrency/infrastructure bug survived a fully-passing test suite
because the test double (EF Core `InMemory`) doesn't implement the same
code paths as the real provider. The `RacyReservationRepository` fake in
`docs/DEBUGGING.md` was a deliberate, deterministic substitute for exactly
this reason; the execution-strategy bug here is the opposite lesson —
sometimes there's no substitute for running against the real thing at least
once.

Frontend was still only checked structurally (build, lint, DOM inspection
of the FindLocker page's loading/error states) rather than a full
click-through of all four screens against live data — that remains
unverified.
