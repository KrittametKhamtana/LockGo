# Deliverables

Maps this folder to the assessment's submission checklist (Source Code /
Database Schema & Migration / README / API Documentation / Test /
Architecture Diagram / AI Prompt / AI Workflow / AI Generated Code
Review).

| Checklist item | Status | Where |
|---|---|---|
| Source Code | ✅ | Whole repo (`LockGo.Api/`, `frontend/`) |
| README | ✅ | [`../README.md`](../README.md) |
| Database Schema / Migration | ✅ | [`DATABASE.md`](DATABASE.md) |
| API Documentation | ✅ | [`API.md`](API.md) (full request/response shapes — the root README's [§9](../README.md#9-api-documentation) stays as the summary) |
| Test | ✅ | [`TESTING.md`](TESTING.md) |
| Architecture Diagram | ✅ | [`ARCHITECTURE.md`](ARCHITECTURE.md) (current structure) |
| AI Workflow | ✅ | [`AI-WORKFLOW.md`](AI-WORKFLOW.md) |
| AI Prompt | ✅ | [`AI-PROMPTS.md`](AI-PROMPTS.md) (the prompt sequence used, with the goal behind each one) |
| AI Generated Code Review | ✅ | [`AI-CODE-REVIEW.md`](AI-CODE-REVIEW.md) (human review of the AI-written booking flow, UI → Service) |

## How the two AI-review documents differ

Both exist on purpose and cover different code:

- [`AI-CODE-REVIEW.md`](AI-CODE-REVIEW.md) — **a human reviewing the AI's
  code**, written in Thai. Covers the booking flow from the screen down to
  the service (`ReservationPage` → `useCreateReservation` → API →
  `ReservationService.CreateAsync`), checked against all five required
  points: correctness, bugs, security, performance, maintainability. Found
  a real bug the 60-test suite can't catch.
- [`AI_USAGE.md`](AI_USAGE.md) §3 — **the AI reviewing its own code**,
  covering a different section: the internals of the reservation
  transaction (`CreateInTransactionAsync` + `EfUnitOfWork`).

## Everything else in this folder

- [`DEBUGGING.md`](DEBUGGING.md) — a real race condition found and fixed
  while writing the concurrency tests.
- [`DEPLOYMENT.md`](DEPLOYMENT.md) — Docker images, GitHub Actions CD,
  DuckDNS + Caddy.
- [`AI_USAGE.md`](AI_USAGE.md) — the original full write-up this folder's
  docs were extracted from. Still the only place covering the live-Postgres
  pass and the two bugs it caught (§4).
