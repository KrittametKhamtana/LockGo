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
| AI Prompt | ⏳ Deferred | Not yet split out — see note below |
| AI Generated Code Review | ⏳ Deferred | Not yet split out — see note below |

## Note on the two deferred items

[`AI_USAGE.md`](AI_USAGE.md) (kept from before this reorganization) already
contains a draft of both — the actual prompts used (§1) and a self-review
of the highest-stakes code section (§3) — but neither has been polished
into its own standalone deliverable yet. Splitting them out is tracked
separately, not abandoned.

## Everything else in this folder

- [`DEBUGGING.md`](DEBUGGING.md) — a real race condition found and fixed
  while writing the concurrency tests.
- [`DEPLOYMENT.md`](DEPLOYMENT.md) — Docker images, GitHub Actions CD,
  DuckDNS + Caddy.
- [`AI_USAGE.md`](AI_USAGE.md) — full prompt/workflow/code-review write-up
  this folder's docs were extracted and updated from.
