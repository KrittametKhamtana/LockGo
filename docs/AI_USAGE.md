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
| **Performance** | The overlap query filters `CompartmentId + Status + StartTime + EndTime` — without a matching index this is a sequential scan on every booking attempt, which matters on a CPU-limited free-tier server. | Composite index `(compartment_id, status, start_time, end_time)` added in `ReservationConfiguration`; verify via `EXPLAIN ANALYZE` against production data volume once seeded — not done here since no live Postgres instance was available in this environment (see the README's "What wasn't verified" note). |
| **Maintainability** | `IdempotencyKeyConflictException` is an internal signal (not a subtype of the public `AppException` hierarchy) that should never reach the API layer unhandled. | Documented explicitly in its own doc comment; if it ever *does* leak, the exception handler's fallback (bare `500 INTERNAL_ERROR`, logged) makes that visible rather than silently mismapping it to a wrong HTTP status. |
| **Maintainability** | `ReservationService` depends on `IUnitOfWork`, `IReservationRepository`, `ICompartmentRepository` — none of which reference EF Core — so the whole class can be unit-tested with hand-written fakes/Moq without a database. Verified this actually holds by writing `ReservationServiceTests.cs` against mocks and confirming zero references to `Microsoft.EntityFrameworkCore` crept into `LockGo.Application.csproj`. | No fix needed — flagged as a thing to *keep* true as the codebase grows, not a defect. |

## 4. What wasn't verified

Being transparent about the boundary of what this session could actually
check:

- **No live Postgres instance.** The spec describes a DB already
  provisioned on a free-tier server; this environment had no credentials
  for it. Docker was available but its daemon wasn't running and starting
  it mid-session didn't complete in time to run a real end-to-end pass
  against Postgres specifically. Everything that depends on Postgres-
  specific behavior (`xmin`, the unique-constraint-violation translation
  path) is covered by the hand-rolled concurrency test instead, which
  models that behavior deterministically without needing the real engine —
  see `docs/DEBUGGING.md` for why that's a reasonable substitute and what
  it can't catch. **Before relying on this in production, run the full
  suite once against a real Postgres instance**, particularly
  `ReservationsApiTests` and the concurrency tests.
- **Frontend was checked structurally** (build, lint, DOM inspection of
  the FindLocker page's loading/error states) rather than with a full
  click-through of all four screens against live data, for the same reason.
