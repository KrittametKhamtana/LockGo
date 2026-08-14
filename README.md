# LockGo — Find & Reserve Locker

Smart Locker platform (mock company: LockGo). A **Find & Reserve Locker**
feature — search, view detail, reserve, confirm — built as a technical
assessment.

```
Search Locker → View Detail → Reserve → Confirmation
```

## Contents

- [Architecture](docs/ARCHITECTURE.md) — layering, diagram, the
  concurrency-critical reservation path, availability model
- [API documentation](docs/API.md)
- [Debugging write-up](docs/DEBUGGING.md) — a real race condition found and
  fixed while writing the concurrency tests
- [AI usage](docs/AI_USAGE.md) — prompts, what the AI decided vs. what was
  specified, a self code-review
- This README — overview, setup, running, testing

## Tech stack (and why)

| Layer | Choice | Why |
|---|---|---|
| Frontend | React + Vite + TypeScript + MUI | Fast dev loop, typed end-to-end, MUI covers the form-heavy screens (filters, selectors, summaries) without hand-rolling components |
| Backend | .NET 10 (latest LTS), EF Core + Npgsql | Strong typing and EF Core migrations give a clear, reviewable DB schema history; Npgsql is the standard Postgres provider |
| Database | PostgreSQL | Specified — already provisioned on a free-tier server, hence the low-connection-pool, index-conscious design throughout |
| CI | GitHub Actions | No self-hosted agent needed, unlike Jenkins — the free-tier server can't spare RAM for one |

MUI is pinned to **7.3.11** rather than the newest `9.x` tag — `9.3.1`'s
type definitions broke `Stack`/`Typography` prop typing under current
TypeScript in a way that failed the build. `7.3.11` is the latest *stable*
major release. See [`docs/AI_USAGE.md`](docs/AI_USAGE.md) for the full story.

## Project structure

```
LockGo.Api/              .NET solution (see docs/ARCHITECTURE.md for the layering)
  LockGo.Api/             Controllers, Program.cs, DI, error middleware
  LockGo.Application/     Services, DTOs, repository interfaces
  LockGo.Domain/          Entities, enums
  LockGo.Infrastructure/  EF Core DbContext, migrations, repositories
  LockGo.Tests/           xUnit: unit, concurrency, integration
frontend/                 React + Vite + TS + MUI
docs/                     Architecture, API docs, debugging write-up, AI usage
.github/workflows/        CI (build + test, both projects)
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- A PostgreSQL database (local, Docker, or a hosted instance) — this project
  targets a free-tier server, so keep the connection pool small if you
  provision your own
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

## Database setup

1. Create a database (any name — `lockgo` is used below).
2. Copy `LockGo.Api/LockGo.Api/appsettings.Development.json.example` to
   `appsettings.Development.json` (gitignored — never commit real
   credentials) and fill in the `Database` section:

   ```json
   {
     "Database": {
       "Host": "localhost",
       "Port": 5432,
       "Database": "lockgo",
       "Username": "lockgo",
       "Password": "YOUR_PASSWORD",
       "MaximumPoolSize": 10,
       "SslMode": "Require"
     }
   }
   ```

   Connection details are structured fields rather than one raw ADO.NET
   connection string, built into one via `NpgsqlConnectionStringBuilder` in
   `LockGo.Infrastructure.DependencyInjection`. Set `SslMode` to `Disable`
   for a local Postgres without TLS; hosted providers (Neon, Supabase, RDS,
   etc.) generally need `Require` or stronger.
   `appsettings.json`'s `Database` section is intentionally left blank —
   that file **is** committed, so no real credentials belong there.

3. Apply migrations. The app does this automatically on startup outside the
   `Testing` environment (see `Program.cs`), or run it manually:

   ```bash
   cd LockGo.Api
   dotnet ef database update --project LockGo.Infrastructure --startup-project LockGo.Api
   ```

4. Seed data (a mock user + a few sample lockers/compartments) is inserted
   automatically on startup, idempotently — see `DbSeeder.SeedAsync`.

## Running the backend

```bash
cd LockGo.Api
dotnet run --project LockGo.Api --launch-profile http
```

- API: `http://localhost:5117`
- Swagger UI: `http://localhost:5117/swagger` (Development only)

## Running the frontend

```bash
cd frontend
cp .env.example .env   # adjust VITE_API_BASE_URL if the API isn't on :5117
npm install
npm run dev
```

Opens on `http://localhost:5173`. The backend's CORS policy allows this
origin by default (`AllowedOrigins` in `appsettings.json`).

## Running tests

```bash
cd LockGo.Api
dotnet test
```

32 tests: unit tests for `ReservationService`'s and `LockerService`'s
business rules (reserve success / no-availability / double-booking
prevention / idempotent replay / per-size availability), a concurrency suite
proving a double-clicked Confirm button can't create two reservations *and*
that concurrent requests for the same size correctly consume separate
compartments rather than colliding (see `docs/DEBUGGING.md`), and
HTTP-level integration tests via `WebApplicationFactory`.

Frontend:

```bash
cd frontend
npm run lint
npm run build   # also type-checks (tsc -b)
```

## Verified against a live database

The full flow (search → detail → reserve → confirm, including firing two
genuinely concurrent double-click requests) has been run against a real
hosted Postgres instance (Neon), not just the test suite's InMemory
provider. That pass caught two real bugs that every automated test had
missed — an `EnableRetryOnFailure`/manual-transaction conflict that made
every reservation fail with a 500, and a `size`+`availability` filter
combination bug — both fixed and now covered by tests. Full story in
[`docs/AI_USAGE.md`](docs/AI_USAGE.md#4-live-postgres-verification--and-two-real-bugs-it-caught).

Not yet done: `EXPLAIN ANALYZE` against a realistic data volume (the seed
data is only a handful of rows), and a full click-through of all four
frontend screens against live data.

## Git workflow

This repo is set up for: `Issue → Branch → Dev → AI-assisted coding → Test
→ Commit → PR → Review → Merge`. In practice for this assessment: one
continuous AI-assisted build session (see `docs/AI_USAGE.md`), verified with
`dotnet test`/`npm run build`/`npm run lint` before commit, on `main`. For
follow-on work, branch per feature/fix (`feature/...`, `fix/...`), open a
PR against `main`, and let the CI workflow (`.github/workflows/ci.yml`) gate
the merge.
