# Northwind Support Desk — Verdentra Internship Assessment

.NET 10 Web API · React 19 / TypeScript · SQL Server 2022. Fork: <https://github.com/SahanAdithya/northwind-support>. See `ASSIGNMENT_CANDIDATE.md` for the brief and `APPROACH.md` for implementation reasoning.

## Implemented and rules understood

- **Task 1 — DEFECT-117 and filtering:** The old `useTicketList` request effect observed search/page/pageSize but **not the dropdown filters**, so changing Status alone did not call the API. The fix includes every non-search filter in the request dependencies, delays search by 300 ms, handles page reset after debounce, and ignores stale responses. Regression tests assert the **outgoing API request**, including search from page 2. Backend `TicketQueries.ApplyFilters` composes SQL `Where` predicates for status, priority, category, customer, agent, unassigned-only, trimmed title/reference/customer search and `slaStatus`; all filters use AND **before** `CountAsync` and `Skip/Take` so count/paging reflect SQL results. A new filter is one more query DTO property and predicate.
- **Task 2 — triage, escalation, SLA:** Requested priority defaults to Medium; flagged categories force Critical. Configured windows are Critical 4 h, High 8 h, Medium 24 h, Low 72 h; Premium halves the window with a one-hour floor. Eligible assignees are active, under their open-ticket limit and specialised if the category requires it; choose fewest open, tie-break by ID. No eligible agent returns a created, unassigned ticket with a reason. Escalation increases one level (except Critical/Resolved/Closed), resets due time from **escalation time**, retains an eligible agent or reassigns, and saves immutable before/after history with actor and reason. Derived SLA states use an injected clock and a shared text-labelled UI indicator.
- **Task 3 — login:** Agent credentials use salted PBKDF2-HMAC-SHA256 (210,000 iterations), hashed during development seeding. Login issues a **60-minute JWT**; invalid/inactive logins return generic 401. Central fallback authorization guards business endpoints; login, development Swagger and `/health` are public. The escalator comes from JWT claims, not the request. React has `/login`, protected routes, return navigation, bearer-token attachment, logout and 401 handling.
- **Task 4 — Docker:** Multi-stage .NET 10 SDK/runtime Dockerfile with restore-layer structure; runtime runs as non-root on HTTP 8080. Docker Compose binds `localhost:5080`, waits for SQL health, passes JWT/connection/environment variables at **runtime** and migrates/seeds an empty development database. `.env` and development-only appsettings are excluded from the published image.

## Quick start (reviewer instructions)

Prerequisites: .NET SDK 10, Node.js >=20 (compatible with installed Vite), Docker Desktop, Git.

```bash
cp .env.example .env                  # local-only fixtures; never commit real secrets
npm ci                                 # if npm cache permissions fail: npm ci --cache "$HOME/.npm-assessment-cache"
docker compose up -d sqlserver         # wait for healthy: docker compose ps
npm run api                            # terminal 1: development API, auto migrate/seed
npm run web                            # terminal 2: http://localhost:5173
```

**Development login:** `alex.turner@northwind-support.example` / `DevOnly!Support2026` (development fixture for seeded agents; Yuki Tanaka is inactive). Local API: `http://localhost:5043`; development Swagger: `https://localhost:7043/swagger`.

**Docker API alternative** (do not run local API on the same frontend proxy concurrently):

```bash
docker compose up -d --build           # SQL + containerised API
curl -i http://localhost:5080/health  # expect HTTP 200
VITE_API_PROXY_TARGET=http://localhost:5080 npm run web
```

API requests to `/api/tickets` require login/JWT. `.env.example` lists `MSSQL_SA_PASSWORD`, `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__Default`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`; Compose supplies the SQL service-name connection. The local source development JWT value is a throwaway fixture; **it is not in the published Docker image**. `docker compose down -v` **permanently deletes the local SQL volume**. In production, perform migrations in a reviewed, single release step with backups, not during startup of multiple API replicas.

## Tests and checks — locally verified 16 October 2026

| Check | Result |
| --- | --- |
| `dotnet build` / `dotnet test` | Build succeeded; **49/49** backend tests passed |
| `npm run lint`, `npm run build --workspace apps/web` | Passed (frontend TypeScript compilation included) |
| `npm run test --workspace apps/web` | **14/14** frontend tests passed; React `act(...)` warnings remain |
| Fresh SQL development database | Migrations applied; seeded **6 customers, 6 agents, 40 tickets** |
| Browser workflows | Login; TCK-0041 created (Premium Outage → Critical, Ben Osei, 2 h); TCK-0023 escalated Medium→High, reassigned Sara Lindqvist, SLA 8 h and history persisted after refresh; Status=New + Priority=High returned 2 rows; Clear filters restored list |
| Docker / API security smoke checks | SQL healthy; API health **HTTP 200**; API non-root UID **1654**; development appsettings absent from runtime; unauthenticated tickets **401**; wrong password **401** |

Docker Desktop image readings: **API runtime 484 MB disk / 122 MB content**, **.NET 10 SDK 1.35 GB disk / 343 MB content** (local image measurements, not registry download sizes). Some build COPY layers were cached, but the specific **restore-cache check after changing only a `.cs` file was not completed**. Repeat a Docker build after a source-only edit and inspect the `RUN dotnet restore` line to validate caching.

Tests cover request regression and search debounce, SQL priority/combined filters/customer search, SLA boundaries and agent selection, escalation/validation, password hashing, React authentication handling and five SLA states. SQL was inspected at the query-composition level (`IQueryable` predicates before count/paging), in automated tests and manually via the filtered UI; no SQL Server execution plan or large-scale load test was performed.

## Architecture, storage and decisions

- **CQRS-like separation:** `TicketQueries` projects `AsNoTracking` SQL reads; `TriagePolicy`/`SlaOptions` hold reusable, testable create/escalate rules rather than duplicating logic in controllers. Injectable clock and config make SLA tests deterministic.
- **Deterministic policy:** specialist and capacity constraints, smallest open count and ID tie-break are explainable. Rejected **client-side filtering** (incorrect counts), **hard-coded category names** and **stored SLA status** (becomes stale).
- **EF migrations:** `TicketEscalations` records ticket FK, previous/new priority, agent, due time, reason, actor and UTC time. `(TicketId, EscalatedAtUtc)` supports newest-first ticket history; agent FKs are indexed. `Agents.PasswordHash` stores salted hashes; `Tickets.SlaStartedAtUtc` anchors recalculated SLA. A **no-op `SyncLatestModel` migration** aligns the EF model snapshot; fresh database migration passed.
- **Authentication:** Existing Agent doubles as login identity (separate registration/roles not required). Generic 401, fallback authorization and JWT-derived escalation actor prevent obvious impersonation. JWT is in browser **localStorage** for reload persistence, but XSS exposure makes it weaker than a secure HttpOnly-cookie/CSRF strategy for production.

## AI use and known limitations

AI was used to review the brief and starter code and draft patches, tests and documentation. The SQL-first filtering and separated triage policy were retained. **Corrections after running tools:** React effect lint errors, missing .NET options-binding dependency, EF model/migration mismatch, source-only search debounce page-reset bug and accidental publication of development config were fixed; local builds, tests and selected runtime flows were rerun. Code and decisions should be explainable during the review. AI estimates are not actual time spent.

Development-verified, **not production-hardened**: concurrent creations may over-assign without transactional locking; tokens in localStorage have XSS exposure and no revocation/refresh; no roles, MFA, registration, password reset or rate limiting (out of scope). Inactive-agent and expired-token journeys, SQL Server plans/high-volume load and the source-edit Docker cache experiment were not exhaustively tested. NuGet reported **NU1902/NU1903 for `System.Security.Cryptography.Pkcs` 9.0.11**; npm audit found **one high-severity issue**; dependency remediation remains. React testing emitted `act(...)` warnings, despite passing tests.

**Submission:** Email the fork link above, development login credentials and **actual time spent on Tasks 1–4**. Keep focused Git commits; further explanation is in `APPROACH.md`.
