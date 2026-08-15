# AI Workflow

How this project was actually built: one continuous agentic session with
Claude Code (Claude Sonnet 5), working from a written project spec rather
than turn-by-turn chat. The AI decomposed the spec into sub-goals and
executed them self-directed; the human reviewed results and steered at
decision points rather than dictating every step.

> **Scope note:** this document covers the *workflow* only. The actual
> prompt text and the standalone AI-generated-code review are separate
> deliverable items, tracked but not included here yet.

```mermaid
flowchart TD
    A(["Written project spec<br/>tech stack, screens, API shape,<br/>data model, business rules 1-4"]) --> B[AI decomposes spec into sub-goals]
    B --> C["Scaffold backend + frontend<br/>(latest tooling per spec)"]
    C --> D{Build issue?}
    D -- "MUI 9.3.1 breaks<br/>TS prop typing" --> E["Pin to 7.3.11<br/>(latest stable major)"]
    D -- ok --> F
    E --> F[Implement business rules 1-4]
    F --> G["Write tests, incl.<br/>concurrency suite"]
    G --> H{"Concurrency test actually<br/>proves the guarantee?"}
    H -- "No — passed for the wrong reason<br/>(fake resolved synchronously)" --> I["Rewrite fake with real<br/>thread sync (Barrier)"]
    I --> G
    H -- Yes --> J["Verify in a real browser<br/>(not just dotnet build)"]
    J --> K["Human review<br/>at decision points"]
    K --> L["Live Postgres becomes available"]
    L --> M["Run full flow against<br/>the real database"]
    M --> N{"Bugs the InMemory<br/>provider couldn't reveal?"}
    N -- "Yes — 2 found" --> O["Fix + add regression tests"]
    O --> P["Self code-review of the<br/>highest-stakes section"]
    N -- No --> P
    P --> Q(["Deliverable"])
```

## Human-decided vs. AI-decided

**Human-decided (in the spec):** feature scope and user flow; exact tech
stack; the data model and field names; all four business rules, including
the specific mechanism mandated for rule 4 (idempotency key + optimistic
concurrency, not pessimistic locking); the hybrid availability model; the
error-shape contract; the testing bar ("prove double-click doesn't
duplicate"); the deliverables checklist itself.

**AI-decided (this session):** the concrete Clean-Architecture layering
and repository/`IUnitOfWork` abstractions; the specific EF Core mechanics
for mapping `xmin`; translating Postgres exceptions into the app's HTTP
error shape; the entire test suite's design, including the hand-rolled
concurrency fake; every line of application/UI code; dependency version
choices; this documentation set.

Full narrative — the two real bugs the live-database pass caught, and the
concurrency test that initially passed for the wrong reason — in
[`AI_USAGE.md`](AI_USAGE.md) and [`DEBUGGING.md`](DEBUGGING.md).
