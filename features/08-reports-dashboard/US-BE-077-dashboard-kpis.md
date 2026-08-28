# US-BE-077 — Dashboard KPIs Endpoint

## Technology Requirements

**Frontend (if applicable):**
- **UI Framework:** Angular Material ONLY (NOT Tailwind, NOT Bootstrap)
- **RTL/LTR:** Must support both RTL (Arabic) and LTR (English)
- **Arabic:** All user-facing text must be translatable to Arabic
- **English:** All user-facing text must be in English
- **i18n:** Use Angular's built-in i18n (`@angular/localize`)

**Backend (if applicable):**
- **Framework:** .NET 10, C#
- **API:** RESTful with OpenAPI
- **Language:** C# with Arabic/English string resources

---


**Epic:** Reports & Dashboard
**Roles:** Admin, Manager, Agent
**As a** manager, **I want to** see a live KPI snapshot on the dashboard, **so that** I can monitor operations at a glance.

## Acceptance Criteria
- [ ] `GET /dashboard/kpis` computes at query time: openTickets (total + by priority), slaBreachRate, avgFirstResponseMinutes (7-day rolling), avgResolutionMinutes (7-day rolling), csatScore (30-day rolling), agentUtilization, ticketsTodayCreated, ticketsTodayResolved, escalationRate, unassignedTickets, agentWorkload[]
- [ ] Admin without `?departmentId` sees org-wide; Manager sees their dept; Agent sees personal KPIs only (no agentWorkload array)
- [ ] `agentUtilization = (agents with ≥ 1 open ticket) / (Available + Busy agents) × 100`
- [ ] `calculatedAt` timestamp included in response

## Technical Notes
- Endpoint: `GET /dashboard/kpis`
- Entity: `Ticket`, `TicketSla`, `CsatSurvey`, `User`
- Business rules: BR-RPT-012, `specs/features/08-reports-dashboard.md`
- Spec: `specs/api/reports.md`

## Dependencies
- US-BE-019, US-BE-039, US-BE-007
