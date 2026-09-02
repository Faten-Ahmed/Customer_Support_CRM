# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Status

This project follows **Spec-Driven Development (SDD)**. No business feature is implemented without a written, approved specification. As of now:

- Phases 1–3 (Requirements, Architecture, API Design): **Complete**
- Phase 4 (Feature Specifications): **Complete** — `features/NN-<name>/spec.md`
- Phase 5 (Implementation Plans): **Complete** — `features/NN-<name>/plans/` (42 FE + 96 BE stories across 13 feature folders)
- Phase 6 (Implementation): **In progress** — Feature 1 (Auth/Login) complete

Both workspaces are scaffolded: Angular 21 at `src/frontend/`, .NET 10 solution at `src/backend/`.

---

## Planned Commands (once scaffolded)

### Frontend (Angular — `src/frontend/`)
```bash
npm install                        # Install dependencies
npx @angular/cli@21 serve          # Dev server at http://localhost:4200 (with API proxy)
npx @angular/cli@21 build --configuration production
npx @angular/cli@21 test --watch=false              # All unit tests (Vitest)
npx @angular/cli@21 test --watch=false --include=src/app/path/to/file.spec.ts  # Single spec
```

### Backend (.NET — `src/backend/`)
```bash
dotnet restore
dotnet build
dotnet run --project CRM.API
dotnet test
dotnet ef database update --project CRM.Infrastructure
```

### Infrastructure
```bash
docker compose up -d               # SQL Server, Redis, MinIO, Seq
```

---

## Architecture

### Full Stack Overview

```
Angular SPA (port 4200)
  ├── REST → ASP.NET Core Web API (port 5000, prefix /api/v1)
  └── WebSocket → SignalR hubs (/hubs/notifications, /hubs/chat, /hubs/dashboard)

API Layer (.NET 10, Clean Architecture)
  CRM.Domain        — Entities, Value Objects, Domain Events
  CRM.Application   — Use Cases, DTOs, Validators
  CRM.Infrastructure — EF Core (SQL Server), Redis, S3, Hangfire
  CRM.API           — Controllers, Middleware, SignalR Hubs, DI wiring

Infrastructure
  SQL Server 2022   — Primary store (EF Core, code-first migrations, temporal tables)
  Redis 7           — Cache + SignalR backplane
  MinIO (dev)       — S3-compatible file storage (pre-signed URLs only)
  Hangfire          — Background jobs (SLA checks, notification dispatch, AI queues)
```

### Frontend Architecture (Angular 21)

All components are **standalone** (no NgModules). Lazy-loaded route groups:

| Route prefix | Purpose |
|---|---|
| `/login`, `/forgot-password` | Unauthenticated auth pages |
| `/change-password` | First-login password change (guarded by `PasswordChangeGuard`) |
| `/app/**` | Internal shell (`AppShellComponent`) — agents, managers, admins |
| `/portal/**` | Customer portal shell (`PortalShellComponent`) — customers only |
| `/app/admin/**` | Admin-only section inside `AppShellComponent` |

**State management rule:** Use Angular Signals for all component and service state. Only introduce NgRx when state is shared across 3+ unrelated components AND involves complex async coordination.

**Key shared services (once implemented):**
- `AuthStore` — signal-based JWT store; `token`, `user()`, `isAuthenticated()`, `passwordMustChange()`
- `SignalRService` — singleton hub connection pool (`Map<string, HubConnection>`)
- `AuthInterceptor` — attaches Bearer token, handles 401 silent refresh, 423 → `/change-password`
- `I18nService` — `signal<'en'|'ar'>`, sets `document.documentElement.dir` and `lang`, persists to `localStorage`

---

## Implementation Plans

All implementation plans are complete and organised by feature. Each feature lives under `features/<name>/`:

```
features/
├── 00-auth/                     # BE-001–008, BE-094 · FE-001–005, FE-042
├── 01-customer-management/      # BE-009–018, BE-096 · FE-006–008
├── 02-ticket-management/        # BE-019–032, BE-037 · FE-009–016
├── 03-sla-management/           # BE-033, BE-038–044 · FE-017, FE-029
├── 04-knowledge-base/           # BE-045–052 · FE-024–025, FE-035
├── 05-notifications/            # BE-053–057 · FE-022–023
├── 06-agent-dashboard/          # BE-034–036, BE-058–062 · FE-018–021
├── 07-admin-configuration/      # BE-063–072 · FE-026–028
├── 08-reports-dashboard/        # BE-073–079 · FE-030–031
├── 09-customer-portal/          # BE-080–081 · FE-032–034, FE-037
├── 10-ai-features/              # BE-083–087 · FE-038, FE-040–041
├── 11-communication-channels/   # BE-088–091 · FE-039
└── 12-csat-surveys/             # BE-082, BE-092–093, BE-095 · FE-036
```

Each folder contains:
- `spec.md` — feature behavioural spec
- `US-BE-NNN-*.md` / `US-FE-NNN-*.md` — user stories
- `plans/US-BE-NNN-plan.md` / `plans/US-FE-NNN-plan.md` — TDD implementation plans

Each plan contains exact file paths, full failing test code, full implementation code, test commands, and git commit commands. Follow plans **task-by-task**: write failing tests → verify they fail → implement → verify they pass → commit.

The canonical plan format is `features/00-auth/plans/US-FE-003-forgot-reset-password.md`.

---

## API Conventions

- Base URL: `http://localhost:5000/api/v1` (dev), `https://crm.azmsquad.com/api/v1` (prod)
- All IDs are UUID v4 strings
- All timestamps are ISO 8601 UTC; display in KSA time (UTC+3)
- Paginated responses include `meta: { page, pageSize, totalCount, totalPages }`
- Error body: `{ errors: [{ code, message, field? }] }`
- Key domain error codes: `409 OPEN_TICKET_EXISTS`, `422 INVALID_CURRENT_PASSWORD`, `503 AI_PROVIDER_UNAVAILABLE`, `423` (password must change)
- File uploads use `multipart/form-data`, max 5 MB, served via pre-signed S3 URLs
- SignalR JWT: passed as `?access_token=<token>` query param on WebSocket handshake

### Role Hierarchy
`Admin > Manager > Agent > Customer` — higher roles inherit lower-role permissions unless explicitly restricted.

---

## Testing Conventions (Frontend)

- Framework: **Vitest + Angular TestBed** — Angular 21 CLI uses `@angular/build:unit-test` which defaults to Vitest
- HTTP: `provideHttpClient()` + `provideHttpClientTesting()` + `HttpTestingController`
- Animations: always import `NoopAnimationsModule`
- Routing: `provideRouter([])` (not `RouterTestingModule` which is deprecated)
- Mocking: use `vi.fn()` and `vi.spyOn()` (Vitest API), NOT `jasmine.createSpyObj`
- Assertions: `toBe(true/false)` not `toBeTrue()/toBeFalse()` (Jasmine-only matchers)
- async tests: use `async/await` + `fixture.whenStable()` — `fakeAsync`/`tick` may not work in Vitest
- Never import real `HttpClient` in component tests
- SignalR connections: mock as `jasmine.createSpyObj('HubConnection', ['start', 'on', 'invoke', 'stop'])` with `start.and.returnValue(Promise.resolve())`
- Run a single spec: `ng test --include=src/app/path/to/file.spec.ts --watch=false`

---

## Key Spec Files

| File | Content |
|---|---|
| `specs/architecture/technology-stack.md` | Fixed stack with full justifications — read before proposing changes |
| `specs/api/overview.md` | Auth, pagination, error format, SignalR hubs |
| `specs/api/auth.md` | Login, token refresh, portal registration endpoints |
| `specs/api/tickets.md` | Ticket lifecycle, messages, attachments, SLA |
| `specs/domain/domain-model.md` | Entities, value objects, domain events |
| `specs/domain/bounded-contexts.md` | Bounded context map |
| `specs/database/schema.md` | Database schema spec |
| `features/NN-<name>/spec.md` | Per-feature behavioral specs (12 files, one per module) |
| `features/NN-<name>/plans/` | TDD implementation plans — 42 FE + 96 BE |

Any change to the technology stack requires an Architecture Decision Record in `specs/decisions/`.
