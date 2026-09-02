# AZM Squad Customer Support CRM

A full-featured, enterprise-grade Customer Support CRM built for Arabic and English markets. Supports multi-department and multi-branch operations, omni-channel communication (Email, WhatsApp, Live Chat, SMS, Web Forms), AI-assisted ticket handling, SLA automation, and a self-service customer portal.

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend language | C# 13 / .NET 10 (LTS) |
| API framework | ASP.NET Core Web API |
| Architecture | Clean Architecture + DDD |
| ORM | Entity Framework Core 10 |
| Database | Microsoft SQL Server 2022 |
| Authentication | ASP.NET Core Identity + JWT |
| Real-time | SignalR |
| Background jobs | Hangfire |
| Cache | Redis 7 |
| File storage | S3-compatible (MinIO for dev) |
| Logging | Serilog |
| Backend tests | xUnit + Testcontainers |
| Frontend framework | Angular 21 (TypeScript) |
| UI library | Angular Material |
| State management | Angular Signals (NgRx where justified) |
| Forms | Angular Reactive Forms |
| i18n | Angular `@angular/localize` — Arabic (RTL) + English |
| Frontend tests | Vitest + Angular TestBed |
| Infrastructure | Docker + Docker Compose |

Full justifications and version specifications: [`specs/architecture/technology-stack.md`](specs/architecture/technology-stack.md)

---

## Modules

1. Customer Management
2. Ticket Management
3. Communication Channels (Email, WhatsApp, Live Chat, SMS, Web Forms)
4. Agent Dashboard
5. SLA & Automation
6. Knowledge Base
7. AI Features (summaries, suggested replies, auto-categorization, AI chatbot)
8. Customer Portal
9. Reports & Management
10. Security & Administration
11. Integrations (REST API, ERP, Email/SMS/WhatsApp providers)
12. Platform (Arabic/English RTL/LTR, multi-department, multi-branch, custom branding)

Full requirement catalog (83 requirements across 12 modules): [`specs/requirements/product-overview.md`](specs/requirements/product-overview.md)

---

## Development Methodology — Spec-Driven Development (SDD)

This project follows a strict SDD workflow. **No business feature is implemented without a written, approved specification.**

```
Requirements → Specification → Architecture → Design
     → Implementation → Tests → Review → Acceptance Criteria Verification
```

| Phase | Artifacts | Location |
|-------|-----------|----------|
| 1. Requirements | Product overview, Open questions | `specs/requirements/` |
| 2. Architecture | Technology stack, Domain model, DB schema | `specs/architecture/`, `specs/domain/`, `specs/database/` |
| 3. API Design | OpenAPI specs per module | `specs/api/` |
| 4. Feature Specs | Per-feature behavioral specs | `specs/features/` |
| 5. Implementation | Source code | `src/` |
| 6. Tests | Unit, integration, E2E | `tests/` |

Architecture Decision Records (ADRs) for any significant technical decisions are stored in `specs/decisions/`.

---

## Project Structure

```
/
├── docs/                    # Human-readable documentation
│   ├── requirements/
│   ├── architecture/
│   ├── decisions/
│   └── development/
├── specs/                   # Machine-traceable specifications
│   ├── requirements/
│   │   ├── product-overview.md
│   │   └── open-questions.md
│   ├── architecture/
│   │   └── technology-stack.md
│   ├── domain/              # Domain model specs (Phase 2)
│   ├── database/            # Schema specs (Phase 2)
│   ├── api/                 # OpenAPI specs per module (Phase 3)
│   ├── features/            # Per-feature behavioral specs (Phase 4+)
│   └── decisions/           # Architecture Decision Records
├── src/
│   ├── backend/             # ASP.NET Core solution
│   └── frontend/            # Angular workspace
├── tests/
│   ├── backend/             # xUnit test projects
│   └── frontend/            # Jasmine/Vitest + Playwright tests
├── infrastructure/
│   └── docker/              # Docker Compose files
└── README.md
```

---

## Getting Started

**Prerequisites:** .NET 10 SDK, Node.js 22 LTS, Docker Desktop

```bash
# 1. Clone the repository
git clone https://github.com/Faten-Ahmed/Customer_Support_CRM.git
cd Customer_Support_CRM

# 2. Start infrastructure (SQL Server, Redis, MinIO, Seq)
docker compose up -d

# 3. Apply database migrations
cd src/backend
dotnet ef database update --project CRM.Infrastructure

# 4. Start the API (http://localhost:5000)
dotnet run --project CRM.API

# 5. Start the frontend (http://localhost:4200)
cd ../frontend
npm install
npx @angular/cli@21 serve
```

### Running Tests

```bash
# Backend (461 tests)
cd src/backend && dotnet test

# Frontend (487 tests across 80 spec files)
cd src/frontend && npx @angular/cli@21 test --watch=false
```

---

## Current Status

| Phase | Status |
|-------|--------|
| Phase 1 — Requirements & Specs | Complete |
| Phase 2 — Domain Model & Architecture | Complete |
| Phase 3 — API Design | Complete |
| Phase 4 — Feature Specifications | Complete |
| Phase 5 — Implementation | Complete (11 of 13 features — AI features skipped) |
| Phase 6 — Testing | Complete — 461 backend + 487 frontend tests, all passing |

### Implemented Features

| # | Feature | Backend | Frontend |
|---|---------|---------|---------|
| 00 | Auth & Login | BE-001–008, BE-094 | FE-001–005, FE-042 |
| 01 | Customer Management | BE-009–018, BE-096 | FE-006–008 |
| 02 | Ticket Management | BE-019–032, BE-037 | FE-009–016 |
| 03 | SLA Management | BE-033, BE-038–044 | FE-017, FE-029 |
| 04 | Knowledge Base | BE-045–052 | FE-024–025, FE-035 |
| 05 | Notifications | BE-053–057 | FE-022–023 |
| 06 | Agent Dashboard | BE-034–036, BE-058–062 | FE-018–021 |
| 07 | Admin Configuration | BE-063–072 | FE-026–028 |
| 08 | Reports & Dashboard | BE-073–079 | FE-030–031 |
| 09 | Customer Portal | BE-080–081 | FE-032–034, FE-037 |
| 11 | Communication Channels | BE-088–091 | FE-039 |
| 12 | CSAT Surveys | BE-082, BE-092–093, BE-095 | FE-036 |
