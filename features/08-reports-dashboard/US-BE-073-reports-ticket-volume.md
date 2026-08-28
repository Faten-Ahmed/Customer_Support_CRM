# US-BE-073 — Ticket Volume Report

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
**As a** manager, **I want to** see ticket volume trends over a date range, **so that** I can identify busy periods and staffing needs.

## Acceptance Criteria
- [ ] `GET /reports/tickets?dateFrom=&dateTo=` (both required) returns summary, byStatus, byPriority, byChannel, trend array
- [ ] Date range max 365 days; exceeding returns `422`
- [ ] Agent scope: their departments only; Manager: their primary department; Admin: all
- [ ] `?groupBy=day|week|month` controls trend granularity (default: day)
- [ ] `openAtEndOfPeriod`: count of tickets created ≤ dateTo and (ClosedAt > dateTo OR ClosedAt IS NULL)
- [ ] `departmentId` filter respected within caller's scope; out-of-scope dept returns `403`

## Technical Notes
- Endpoint: `GET /reports/tickets`
- Entity: `Ticket`
- Business rules: BR-RPT-001—005
- Spec: `specs/api/reports.md`, `specs/features/08-reports-dashboard.md`

## Dependencies
- US-BE-019, US-BE-007
