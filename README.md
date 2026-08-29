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
| Frontend tests | Jasmine / Vitest + Playwright |
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

> Prerequisites and setup instructions will be added in Phase 2 once the backend solution and Docker Compose configuration are scaffolded.

**Planned setup steps:**
1. Install prerequisites: .NET 10 SDK, Node.js 22 LTS, Docker Desktop
2. Clone the repository
3. Copy `.env.example` to `.env` and configure secrets
4. Run `docker compose up -d` to start SQL Server, Redis, MinIO, and Seq
5. Run database migrations: `dotnet ef database update`
6. Start the API: `dotnet run --project src/backend/CRM.API`
7. Start the frontend: `cd src/frontend && npm install && ng serve`
8. Open `http://localhost:4200`

---

## Current Status

| Phase | Status |
|-------|--------|
| Phase 1 — Requirements & Specs | In Progress |
| Phase 2 — Domain Model & Architecture | Pending |
| Phase 3 — API Design | Pending |
| Phase 4 — Feature Specifications | Pending |
| Phase 5 — Implementation | Pending |
| Phase 6 — Testing | Pending |

Open questions that must be answered before Phase 2: [`specs/requirements/open-questions.md`](specs/requirements/open-questions.md)
