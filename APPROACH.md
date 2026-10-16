# Engineering approach and decisions

This is a supporting decision log for the one-to-three-page-style summary in `README.md`. The goal was to fix causes rather than conceal symptoms, keep policy behaviour testable, and clearly distinguish implementation from locally verified behaviour.

## Task 1 — DEFECT-117 and server-side filtering

Originally `useTicketList` reacted only to search/page/pageSize while its API request carried additional filter fields. Dropdown changes alone therefore did not refetch; typing later carried stale filter values. The fix makes every non-search filter part of the request dependencies, uses a 300 ms search debounce and rejects stale asynchronous responses. A subsequent regression check caught an early fetch when searching on page 2; pagination now resets after the search debounce, not immediately on each keystroke. Test assertions inspect requests, not just UI state.

Backend queries start from a read-only joined `IQueryable`. `ApplyFilters` conditionally adds `Where` clauses for exact fields, null assignee, trimmed substring search and computed SQL-side SLA status; then `CountAsync` and sorted `Skip/Take` operate on that same query. This keeps totals consistent and lets a future filter be added as another predicate. No results were filtered in memory. Tests covered exact priority, combinations and customer names; live UI tests checked Status=New + Priority=High yielding two results. A full SQL Server query-plan/performance audit was not performed.

## Task 2 — triage and escalation

Use category **flags**, not names, to force Critical or specialist routing. The policy chooses from active, eligible agents below their open-ticket cap; fewest open tickets then agent ID produces repeatable decisions. The no-eligible result stays unassigned with a reason instead of failing ticket creation. SLA windows come from bound options (4/8/24/72 hours by priority; Premium half-window; one-hour floor). The injected clock permits boundary tests.

Escalation validates a 5–500 character reason, rejects Critical/terminal tickets, advances one priority level, computes due date from the escalation instant, retains an eligible owner or reselects and writes an immutable history record. Derived SLA status uses nullable `SlaStartedAtUtc` (old records fall back to `CreatedAtUtc`) to avoid treating the new SLA window as if it started at original creation. Shared SLA UI labels include text so state is not expressed by color only. Tests cover boundaries, agent selection and rejection behaviour. Browser tests demonstrated forced Critical creation and persisted Medium-to-High escalation.

Schema: `TicketEscalations` with history indexes and restricted FK deletes; `Agents.PasswordHash` for credentials; `Tickets.SlaStartedAtUtc` for SLA restart. The no-op `SyncLatestModel` migration synchronizes EF metadata to the actual model after avoiding duplicate schema operations. A clean development database successfully migrated and seeded. Concurrent assignments still need transaction-level reservation to enforce strict caps in production.

## Task 3 — authentication

Reuse Agent identities rather than adding a separate registration/roles workflow. Seed a salted PBKDF2-HMAC-SHA256 hash using the framework; return the same 401 wording for unknown/wrong credentials and reject inactive agents. Signed JWTs expire after 60 minutes. Central fallback authorization covers business controllers while login, development Swagger and `/health` remain public. The escalator is read from token claims, avoiding a spoofable browser-supplied field.

The React session client attaches tokens, protects routes, returns users after login and clears credentials on logout/401. `localStorage` is convenient for local reloads but vulnerable to XSS; HttpOnly cookie and CSRF strategy would be preferable for production. Manual checks verified successful login, wrong-password 401 and unauthorized tickets 401. Token expiry and inactive-agent browser journeys were not exhaustively smoke-tested.

## Task 4 — Docker

A multi-stage SDK/runtime Dockerfile keeps SDK tooling out of the final image; project metadata is copied before source to permit `dotnet restore` caching. The runtime uses `$APP_UID` (observed UID 1654) and port 8080; host publishes localhost:5080. Compose waits for SQL health and supplies environment, DB connection string and JWT at runtime. The local development appsettings JSON is explicitly excluded from the published runtime. Fresh development startup migrates/seeds SQL; production should run migrations as a controlled, single release operation.

Verified locally: both services launched, empty database migrated and seeded, `GET /health` HTTP 200, unauthenticated business endpoint HTTP 401, non-root user and absent development JSON. Docker Desktop reported runtime image **484 MB disk/122 MB content**, compared with SDK **1.35 GB disk/343 MB content**. Some Docker build layers showed `CACHED`; the specific restore-after-source-only-change exercise remains outstanding.

## AI collaboration and verification record

AI helped analyze the requirement, propose policy/data-flow structures and draft initial implementation, tests, patches and explanations. The SQL-first query/policy split was accepted as a useful design. Changes were not accepted without checking: lint identified synchronous effect-state problems; .NET compilation found missing options binding; a persistence model test exposed migration snapshot drift; initial Docker packaging could include development config; an additional frontend test exposed a page-2 debounce issue. Follow-up patches addressed these findings. Local evidence: 49/49 .NET and 14/14 Vitest tests passed; frontend lint/build, Docker health, creation/escalation/persistence/filtering smoke checks passed. NuGet/npm security advisories and the remaining test gaps are disclosed in `README.md`.
