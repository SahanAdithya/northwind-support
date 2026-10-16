# Northwind Support Desk — Candidate Assessment

.NET 10 Web API · React 19 / TypeScript · SQL Server 2022. Original assessment in `ASSIGNMENT_CANDIDATE.md`; implementation notes in `APPROACH.md`.

## Implemented source changes

1. **DEFECT-117:** In `useTicketList`, the old effect depended on search and paging only. Changing a dropdown did not cause a request. The fix derives a request object from **every** non-search filter plus a separately debounced search value. Search alone waits 300 ms, while dropdowns fetch immediately; a request counter ignores late responses. Regression tests assert the actual API call.
2. **Database filtering:** `TicketQueries.ApplyFilters` adds SQL `Where` predicates before both `CountAsync` and `Skip/Take`. Status, priority, category, customer, assigned agent, unassigned-only and trimmed search combine by AND; search covers title/reference/customer. `slaStatus` follows the same extension pattern using SQL-side date comparisons. Correct count and page calculations come from the database.
3. **Triage & escalation:** The options-bound `SlaOptions` provides configurable priority windows (Low 72 h, Medium 24 h, High 8 h, Critical 4 h), Premium multiplier 0.5, 1 h floor and an at-risk threshold of 25%. Category flags, not category names, force Critical and specialist assignment. Active specialists below capacity compete by fewest open tickets, with ID tie-break. No match returns a 201 ticket unassigned with a reason. Escalation raises priority exactly one level, starts a new SLA window and records immutable history; Critical and resolved/closed cannot escalate. `SlaStartedAtUtc` allows at-risk status after an escalation to use the **new** window, falling back to `CreatedAtUtc` for historical rows.
4. **Authentication:** Agents authenticate with email/password at `/api/auth/login`. Passwords use PBKDF2-HMAC-SHA256 (210,000 iterations, random 16-byte salt, constant-time verification), salted when seeded for local development. JWTs last 60 minutes. Central fallback authorization protects API controllers; login, Swagger in Development and `/health` are anonymous. The escalation actor comes from the authenticated token. React provides a login form, protected routes, return URL, session header/logout, token attachment and 401 cleanup.
5. **Docker draft:** A multi-stage SDK/runtime `apps/api/Dockerfile` uses restore-layer caching, non-root `$APP_UID`, port 8080; Compose binds host 5080, waits for SQL health, and passes JWT and connection strings at runtime. `/health` checks SQL connectivity.

> **Verification status:** The code was generated/reviewed in an environment without the .NET SDK, Docker daemon, or complete npm dependencies. The C# build, EF migrations, browser tests and Docker runtime **were not executed here**. TypeScript files were parsed for syntax only. Complete the verification checklist below on your own machine before representing this as a working submission; if a test fails, disclose/fix it. Task 4 is *not runtime-verified*.

## Local development

Prerequisites: .NET SDK **10**, Node.js **22.19+** (or compatible with `package-lock.json`), Docker Desktop.

```bash
cp .env.example .env
npm ci
npm run db:up
npm run api                 # terminal A; development migrates and seeds
npm run web                 # terminal B; http://localhost:5173
```

API: http://localhost:5043 · Swagger: https://localhost:7043/swagger (Dev only). Login in the web app with:

- **Email:** `alex.turner@northwind-support.example`
- **Password (all seeded agents):** `DevOnly!Support2026`
- Yuki Tanaka is inactive and cannot sign in. **These are test fixtures, not production credentials.**

The dev signing key in `appsettings.Development.json` is a **public, throwaway fixture**. In other environments supply a new random 32+-byte value via `Jwt__Key`; never copy the dev key into production or commit real secrets. Environment variables: `ConnectionStrings__Default`, `ASPNETCORE_ENVIRONMENT`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`. The access token is stored in `localStorage` for simple tab persistence: straightforward, but vulnerable to XSS. Production should prefer secure, HTTP-only cookies with an appropriate CSRF design and a proper secrets manager. Token lifetime is 60 min, no refresh token or account registration.

### Docker (optional task 4)

```bash
cp .env.example .env
# confirm Jwt__Key and MSSQL_SA_PASSWORD are set in .env
# Make sure Docker Desktop is running
docker compose down -v
docker compose up --build
curl -i http://localhost:5080/health
# Login via API or web first: /api/tickets correctly returns 401 without a JWT
VITE_API_PROXY_TARGET=http://localhost:5080 npm run web
```

**Required checks not carried out in this environment:** Run `docker compose exec api whoami` (must not be `root`), inspect image sizes using `docker image ls`, change a `.cs` file then rebuild to confirm the restore layer is cached; run with a blank SQL volume to confirm migrations and seeding. Report the actual runtime/SDK image sizes only after measuring. Production migrations should run as a controlled release step, not opportunistically when multiple replicas start.

## Test & review checklist

```bash
dotnet restore
dotnet build
dotnet test
npm ci
npm run typecheck
npm run lint
npm run test --workspace apps/web
npm run build --workspace apps/web
```

New tests cover: status-change triggers network request while search is debounced; typed SLA filter hook; SQL priority, combined filters and customer-name search; configurable SLA windows and floor; deterministic specialist selection and no-eligible case; SLA exact boundaries and escalation clock; salted password verification; invalid escalation reason and 409 UI response; all five SLA label states; token attachment and 401 clearing.

**Manual verification:** 1) change only Status and watch Network; 2) combine customer+priority+search and compare returned count; 3) create Standard/Premium and forced-Critical tickets; 4) verify a full/inactive specialist is skipped; 5) escalate, inspect history and due date; 6) sign in/out, retry with wrong password, verify 401 without token; 7) restart with empty DB; 8) run Compose and check `/health`.

## Design decisions and trade-offs

- **CQRS read/query separation:** Keep SQL selection and paging in `TicketQueries` and business decisions in `TriagePolicy` / `SlaOptions` rather than filtering in React or writing rules in controllers.
- **Policy as a pure function:** Assignment and due-window rules are deterministic and unit-testable. An infrastructure projection obtains capacities in the database; the policy selects from that snapshot. This is simple for one API instance but not safe for concurrent high-volume assignment without a transaction/locking strategy.
- **Immutable audit table:** `TicketEscalations` records before/after priority, due time, agent and actor. The compound `(TicketId, EscalatedAtUtc)` index supports per-ticket newest-first history. `Tickets.SlaStartedAtUtc` is nullable for old seed tickets; EF migration also adds `Agents.PasswordHash`. Passwords belong to agents because the assessment does not require a separate user lifecycle.
- **JWT:** Shared fallback policy rather than decorating every controller. Generic login failure and inactive-agent block avoid information leaks. PBKDF2 is from the platform cryptography implementation. No home-made crypto algorithm.
- **SQL filtering:** A single reusable `ApplyFilters()` query composition method; adding a filter is one predicate plus one DTO parameter. SQL translation and execution plans must still be inspected with SQL Server; `%`-style wildcard behavior and indexes will matter at high scale.
- **Rejected approaches:** client-side filtering (breaks total count), delayed dropdown fetch (reproduces defect), stored SLA status (becomes stale), hard-coded category names, plaintext/dev password hashes checked into source, manually setting escalation actors from request body.

## Known limitations and follow-up

- **Crucial:** No .NET compile, database, test-suite, or Docker runtime confirmation was possible in the generation environment. Resolve any compiler/migration/runtime issues before submission. EF model snapshot should be regenerated/checked with `dotnet ef migrations` in a configured development environment before further migrations are authored.
- Agent-capacity selection is based on a snapshot; concurrent ticket creation can over-assign without transaction locking.
- JWT in localStorage is vulnerable to XSS; no refresh, rate limiting or revocation, authorization roles, password resets, OAuth or MFA (outside scope).
- Developer-only seed credentials should never be used on public servers.
- Docker runtime image size / restore-cache validation pending execution; HTTPS termination is not configured inside container by design.

## AI use disclosure

An AI coding assistant was used to inspect the assessment and starter code, propose and draft implementation changes, add tests and draft this documentation. The original effect dependency omission and the developer-experience fixes were inspected directly. Generated code was revised to (a) use the authenticated identity rather than accepting the escalation actor from the browser and (b) set `SlaStartedAtUtc` when escalating so at-risk status reflects the new window. **Not verified**: claims that C# tests or Docker passed. Before sharing with reviewers, the candidate should run the checklist above, review every file and be prepared to explain the actual behavior and limitations.

## Submission

This downloaded ZIP is **not a GitHub fork**. The evaluator requires commits on your fork. Follow `SUBMISSION_GUIDE.txt` to apply the separated patch series to a real fork, run and fix tests, push, then email the fork link, dev credentials and **your actual** time spent per task. Mark any unfinished/unverified work truthfully.
