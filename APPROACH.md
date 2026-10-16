# Approach and decision record

## Task 1 — defect and server-side filters

The dropdown writes a new filter value, but `useTicketList` previously listened only for `[filters.search, filters.page, filters.pageSize]`. Changing `status` alone did not reload; changing search later did, carrying the earlier dropdown value. The fix derives a stable request-input value from all non-search fields. Search is kept in separate debounced state to avoid a request per keystroke, while other changes are immediate. A request sequence number prevents older results replacing newer ones. Tests inspect `ticketsApi.getTickets` calls rather than merely inspecting rendered data.

Backend: apply composable `Where` predicates to a single joined IQueryable before Count/Skip/Take. For the optional Task 2 `slaStatus`, use SQL Server date comparisons with `DateDiffSecond` so the count and page stay consistent. `Contains` expresses substring matching, with SQL Server collation determining case behavior; trimming whitespace is explicit.

## Task 2 — triage and escalation

Triage policy is separated into a pure priority/agent selector and `SlaOptions` bound from configuration. Customer/category data comes from repositories; capacity projection is fetched in one read. Start with requested/default Medium; category flag may override to Critical. Apply SLA window and Premium multiplier/floor. Select among active agents under capacity, specialist when flagged, fewest open, then ID. No candidate is a normal unassigned result. Keep the reason in the create response rather than storing redundant state.

Escalation validates text, rejects terminal/Critical tickets, records an immutable before-and-after audit row, recalculates window from escalation time, keeps an eligible current agent and otherwise reselects. The SLA status is derived when queried from `DueAtUtc`, `ResolvedAtUtc`, current injected clock and `SlaStartedAtUtc` (fallback creation for existing tickets). History is indexed by ticket and escalation time. UI reuses the SLA component across list and detail and refreshes state/history without navigating away.

Trade-offs: simple work-count selection works for this single-instance demo, but true race-free capacity enforcement would require database-level transaction locks or queueing. The original seed date logic is preserved for old tickets. The query is database-driven; no in-memory paging/filtering was introduced.

## Task 3 — authentication

Reuse agents as identities because adding registration and independent roles would exceed scope. Store only a salted PBKDF2-HMAC-SHA256 password hash on agents, created at **seed time**. No real password is committed as a hash or plaintext application secret. A generic 401 response handles wrong credentials and inactive users. Sign JWTs using a config-only key and 60-minute lifetime. A fallback authorization policy protects all ordinary API endpoints, leaving login and `/health` public. The escalation actor is taken from claims rather than the POST body.

Front end: one request client attaches bearer tokens; a session store handles sign-in/out, protected routing and redirect on 401. localStorage simplifies refresh survival but increases impact of XSS; secure HTTP-only cookie design would be preferred for a production system.

## Task 4 — Docker

Multi-stage Dockerfile caches restore after copying solution project metadata, publishes with SDK and runs on small ASP.NET runtime as `$APP_UID` on port 8080. Compose waits for SQL health and passes secrets as runtime environment variables. In Development only, startup migrations/seed permit blank volumes to be usable. `/health` checks SQL reachability. **Not run or measured in this environment**; do not claim runtime image size or a successful Compose run until verified.

## Tests and review

Drafted xUnit tests cover priority windows, floor, forced Critical, specialist selection, ties/capacity, SLA exact boundaries, password verification, and SQL filtering on SQLite; Vitest tests cover regression request shape, SLA indicator states, invalid escalation reason, 409 feedback and auth header/401 handling. Backend/UI end-to-end smoke tests and SQL Server explain plans remain to be completed. Run `dotnet test`, `npm run typecheck`, `npm run test --workspace apps/web`, `npm run lint`, and Docker checks on a local machine. Preserve failing tests and address the actual cause rather than hiding failures.

## AI disclosure

AI assistance was used for static repo analysis, proposing architecture, creating the initial code drafts, preparing tests and documentation. It helped identify missing effect dependencies and introduced a necessary follow-up change: tracking escalation SLA start rather than always using creation time. The candidate must personally review, run, verify and defend the changes. Do not replace actual test results or actual time spent with invented claims.
