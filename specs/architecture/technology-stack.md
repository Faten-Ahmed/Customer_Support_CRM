# Technology Stack — AZM Squad Customer Support CRM

> **Status:** Approved — Fixed Stack (Do Not Change Without ADR)
> **Version:** 0.1.0
> **Date:** 2026-08-24

---

## Decision Record

This technology stack is **fixed and pre-approved**. Any proposed changes must go through an Architecture Decision Record (ADR) in `specs/decisions/`. All implementation work must comply with this stack.

---

## Stack Overview

```
┌─────────────────────────────────────────────────────────┐
│                     CLIENT LAYER                        │
│  Angular 21 (TypeScript) — Angular Material            │
│  RTL/LTR i18n — Reactive Forms — Angular Router         │
│  Signals (primary state) — NgRx (complex flows only)    │
└────────────────────────┬────────────────────────────────┘
                         │ HTTPS / WebSocket
┌────────────────────────▼────────────────────────────────┐
│                     API LAYER                           │
│  ASP.NET Core Web API (.NET 10)                         │
│  OpenAPI / Swagger — JWT Auth — SignalR (real-time)     │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│                 APPLICATION LAYER                       │
│  Clean Architecture — DDD Principles                    │
│  Hangfire (background jobs)                             │
└───────────────┬────────────────────┬────────────────────┘
                │                    │
┌───────────────▼──────┐  ┌──────────▼──────────────────┐
│  PRIMARY STORAGE      │  │  SECONDARY STORAGE          │
│  SQL Server           │  │  Redis (cache + pub/sub)    │
│  Entity Framework Core│  │  S3-compatible (files)      │
└───────────────────────┘  └─────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│                INFRASTRUCTURE                           │
│  Docker + Docker Compose — Serilog (structured logs)   │
└─────────────────────────────────────────────────────────┘
```

---

## Backend

### Language & Runtime

| Component | Choice | Version |
|-----------|--------|---------|
| Language | C# | 13 (with .NET 10) |
| Runtime | .NET | 10 (LTS) |

**Justification:** C# 13 with .NET 10 LTS provides long-term support, excellent performance benchmarks, native async/await, strong typing, and the richest ecosystem for enterprise CRM development. .NET 10 is the latest LTS release offering 3 years of support.

---

### API Framework

| Component | Choice | Version |
|-----------|--------|---------|
| Framework | ASP.NET Core Web API | Included in .NET 10 |
| Documentation | Swagger / OpenAPI | Swashbuckle.AspNetCore 7.x or Scalar |

**Justification:** ASP.NET Core is the industry standard for high-performance, cross-platform .NET APIs. OpenAPI/Swagger ensures the API is self-documenting and enables future frontend or third-party integration without manual documentation effort.

**Configuration notes:**
- All endpoints versioned from day one (`/api/v1/...`)
- Swagger UI enabled in Development environment only
- Scalar or Redoc considered as an alternative Swagger UI

---

### Architecture Pattern

| Component | Choice |
|-----------|--------|
| Architecture | Clean Architecture |
| Design Principles | Domain-Driven Design (DDD) |

**Justification:** Clean Architecture enforces separation of concerns across four layers (Domain, Application, Infrastructure, Presentation), making the codebase testable without an active database and ensuring business logic is never polluted by infrastructure concerns. DDD principles align with the complex domain of a CRM (bounded contexts: Ticketing, Customer, Knowledge, Communication, Identity).

**Layer structure:**
```
src/backend/
├── CRM.Domain/          # Entities, Value Objects, Domain Events, Interfaces
├── CRM.Application/     # Use Cases, DTOs, Validators, Interfaces
├── CRM.Infrastructure/  # EF Core, Redis, S3, Email, SMS, WhatsApp, Hangfire
└── CRM.API/             # Controllers, Middleware, SignalR Hubs, DI setup
```

---

### ORM & Database

| Component | Choice | Version |
|-----------|--------|---------|
| ORM | Entity Framework Core | 10.x (aligned with .NET 10) |
| Database | Microsoft SQL Server | 2022 (containerized for dev) |

**Justification:** EF Core provides a strongly-typed query API, automatic migrations, and tight integration with ASP.NET Core DI. SQL Server 2022 offers enterprise-grade reliability, JSON support, Row-Level Security, and temporal tables — all directly useful for audit logging and multi-tenant data isolation.

**Configuration notes:**
- Migrations are code-first; each migration is reviewed before being applied to staging
- Soft-delete pattern applied globally via query filters
- Temporal tables (SQL Server 2022 feature) used for ticket/customer audit history
- Connection strings are environment-variable-only; never committed to source control

---

### Authentication & Authorization

| Component | Choice | Version |
|-----------|--------|---------|
| Identity | ASP.NET Core Identity | Included in .NET 10 |
| Tokens | JWT (JSON Web Tokens) | System.IdentityModel.Tokens.Jwt |
| Authorization | Policy-based RBAC | Built into ASP.NET Core |

**Justification:** ASP.NET Core Identity provides a production-grade user management foundation (password hashing, lockout, claims). JWT enables stateless, scalable authentication suitable for both the Angular SPA and future mobile clients. Policy-based authorization allows fine-grained permission checks aligned with the RBAC requirement.

**Configuration notes:**
- Access token expiry: configurable (default 15 minutes)
- Refresh token: stored securely; httpOnly cookie for web
- Separate authentication flow for Customer Portal users vs. internal agents
- All tokens include `tenant`, `department`, and `role` claims

---

### Real-Time Communication

| Component | Choice | Version |
|-----------|--------|---------|
| Real-time | SignalR | Included in ASP.NET Core |

**Justification:** SignalR abstracts WebSocket, Server-Sent Events, and long-polling, automatically negotiating the best transport. It integrates natively with ASP.NET Core Identity for authenticated connections. Required for: live chat (REQ-COMM-003), real-time agent notifications (REQ-AGNT-006), and live dashboard updates (REQ-RPT-005).

**Planned SignalR hubs:**
- `ChatHub` — customer live chat
- `NotificationHub` — agent alerts and SLA warnings
- `DashboardHub` — real-time management dashboard data

---

### Background Jobs

| Component | Choice | Version |
|-----------|--------|---------|
| Job scheduling | Hangfire | 1.8.x |
| Storage backend | SQL Server (Hangfire) | — |

**Justification:** Hangfire provides persistent, reliable background job processing with a built-in dashboard for monitoring. Stored in SQL Server (same database), eliminating the need for a separate message broker for CRM workloads.

**Planned Hangfire jobs:**
- SLA countdown and breach checking
- Email/SMS/WhatsApp notification dispatch
- Report generation (async)
- AI processing queues
- Recurring data cleanup and archival

---

### Caching

| Component | Choice | Version |
|-----------|--------|---------|
| Distributed cache | Redis | 7.x (containerized) |

**Justification:** Redis provides sub-millisecond response for frequently read data (session tokens, user permissions, knowledge base articles, dashboard aggregates). It also doubles as a pub/sub bus for broadcasting SignalR events across multiple API server instances in a scaled deployment.

**Cache strategy:**
- Cache-aside pattern for all entity reads
- TTL enforced on all keys; no indefinite caching
- Redis keyspace prefixed by tenant/department for isolation

---

### File Storage

| Component | Choice |
|-----------|--------|
| Object storage | S3-compatible API (e.g., MinIO for dev, AWS S3 / Azure Blob for prod) |

**Justification:** Using an S3-compatible abstraction avoids vendor lock-in. MinIO runs in Docker for local development with identical semantics. In production, the team can switch to AWS S3, Azure Blob Storage, or any compatible provider by changing configuration only.

**Configuration notes:**
- Files are never served directly through the API; pre-signed URLs used
- File metadata (name, size, type, uploader, timestamp) stored in SQL Server
- Virus scanning hook to be defined in OQ-003 resolution

---

### Logging

| Component | Choice | Version |
|-----------|--------|---------|
| Logging framework | Serilog | 4.x |
| Sinks | Console (dev), File, Seq or Elasticsearch (prod) | — |

**Justification:** Serilog's structured logging (key-value pairs, not plain strings) enables log aggregation, filtering, and alerting in tools like Seq, Elastic Stack, or Azure Monitor. Structured logs are essential for diagnosing CRM issues across multiple channels and tenants.

**Configuration notes:**
- Minimum level: `Information` in production, `Debug` in development
- All logs include: `CorrelationId`, `UserId`, `TenantId` (or department/branch)
- PII (customer names, emails) are not logged in plaintext; redacted or hashed

---

### Testing (Backend)

| Component | Choice | Version |
|-----------|--------|---------|
| Unit & integration tests | xUnit | 2.x |
| Mocking | Moq or NSubstitute | Latest stable |
| Test DB | EF Core InMemory / Testcontainers (SQL Server) | — |
| API testing | WebApplicationFactory (integration) | Built into .NET |

**Justification:** xUnit is the dominant .NET test framework, first-class in ASP.NET Core tooling. Testcontainers spins up a real SQL Server container for integration tests, preventing the mock/prod divergence risk.

---

## Frontend

### Framework & Language

| Component | Choice | Version |
|-----------|--------|---------|
| Framework | Angular | 21 |
| Language | TypeScript | 5.x |

**Justification:** Angular's opinionated structure (modules, services, DI, routing) is well-suited to a large enterprise CRM. Strong TypeScript integration catches errors at compile time. The Angular CLI provides consistent project scaffolding, testing, and build tooling across the team.

---

### UI Component Library

| Component | Choice |
|-----------|--------|
| UI Library | Angular Material (MDC-based) |

**Justification:** Angular Material provides accessible, RTL-aware components natively — critical for Arabic UI (REQ-PLT-003). It integrates directly with Angular's theme system, enabling the custom branding requirement (REQ-PLT-008).

---

### State Management

| Component | Choice |
|-----------|--------|
| Primary state | Angular Signals |
| Complex global state | NgRx (introduce only when justified) |

**Justification:** Angular Signals (stable since Angular 17) handle component and service state with minimal boilerplate and excellent performance. NgRx adds complexity and should be introduced only for genuinely complex cross-cutting state (e.g., real-time notification queue, multi-tab synchronization). This prevents over-engineering.

**Decision rule:** if state is needed in more than 3 unrelated components AND involves complex async coordination → consider NgRx. Otherwise use Signals.

---

### API Communication

| Component | Choice |
|-----------|--------|
| HTTP client | Angular HttpClient |
| Real-time | `@microsoft/signalr` (Angular wrapper) |

**Justification:** Angular HttpClient integrates with Angular interceptors, enabling centralized authentication token injection, error handling, and request logging. The official SignalR client library connects seamlessly to the ASP.NET Core SignalR hubs.

---

### Routing

| Component | Choice |
|-----------|--------|
| Router | Angular Router |
| Guards | Auth guard (JWT), Role guard (RBAC) |
| Strategy | Hash-based or PathLocation (TBD with deployment config) |

---

### Forms

| Component | Choice |
|-----------|--------|
| Forms approach | Angular Reactive Forms |

**Justification:** Reactive Forms provide programmatic control over form state, validation, and dynamic field generation — essential for configurable ticket forms, admin settings panels, and knowledge base authoring.

---

### Internationalization

| Component | Choice |
|-----------|--------|
| i18n | Angular built-in i18n (`@angular/localize`) |
| Languages | Arabic (ar), English (en) |
| Text direction | RTL (Arabic), LTR (English) |

**Justification:** Angular's built-in i18n produces separate compiled bundles per locale, delivering full translation without runtime overhead. Angular Material's Directionality service switches RTL/LTR natively. All strings must be externalized from day one — no hardcoded UI text.

---

### Testing (Frontend)

| Component | Choice |
|-----------|--------|
| Unit tests | Jasmine + Angular TestBed (default) or Vitest |
| E2E tests | Playwright (recommended) or Cypress |

**Justification:** Jasmine with Angular TestBed is the Angular-native test setup requiring no additional configuration. Vitest is considered as a faster alternative. E2E tests with Playwright cover critical user journeys (ticket creation, live chat, portal submission).

---

## Infrastructure

### Containerization

| Component | Choice | Version |
|-----------|--------|---------|
| Container runtime | Docker | 26.x+ |
| Local orchestration | Docker Compose | 2.x |

**Justification:** Docker ensures environment parity between developer machines and production. Docker Compose defines the full local development environment in a single file, eliminating "works on my machine" issues.

**Local services (Docker Compose):**

| Service | Image | Port |
|---------|-------|------|
| SQL Server | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 |
| Redis | `redis:7-alpine` | 6379 |
| MinIO (S3) | `minio/minio:latest` | 9000 / 9001 |
| Seq (logs) | `datalust/seq:latest` | 5341 / 8090 |
| API | Custom Dockerfile | 5000 |
| Angular (dev server) | Node.js + Angular CLI | 4200 |

---

## Component Interaction Summary

```
Angular SPA
  ├── REST calls → ASP.NET Core Web API
  │     ├── Application layer → Domain logic
  │     ├── Infrastructure → SQL Server (via EF Core)
  │     ├── Infrastructure → Redis (cache/pub-sub)
  │     ├── Infrastructure → S3-compatible storage (files)
  │     ├── Infrastructure → Email/SMS/WhatsApp providers
  │     └── Hangfire → Background job queue (SQL Server)
  └── WebSocket → SignalR Hubs
        ├── ChatHub (live chat)
        ├── NotificationHub (alerts)
        └── DashboardHub (real-time metrics)
```

---

## Version Pinning Policy

- All NuGet packages pinned to exact minor version in production builds
- All npm packages pinned to exact version in `package-lock.json`
- .NET 10 LTS: upgrade path reviewed annually
- Angular: follow Angular's LTS policy (18 months active support per major version)
- Dependency upgrades require a passing test suite before merging
