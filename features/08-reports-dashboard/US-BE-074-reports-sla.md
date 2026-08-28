# US-BE-074 — SLA Compliance Report

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
**As a** manager, **I want to** see SLA compliance rates for a period, **so that** I can spot systemic SLA failures.

## Acceptance Criteria
- [ ] `GET /reports/sla` returns first response compliance, resolution compliance, byPriority breakdown, breachReasons
- [ ] Only tickets created within the date range are evaluated
- [ ] `avgFirstResponseMinutes` and `avgResolutionMinutes` computed in business minutes
- [ ] `complianceRate` = (met / total) × 100, rounded to 1 decimal
- [ ] Filter: `?priority=Critical|High|Medium|Low` for per-priority drill-down
- [ ] Role scoping same as ticket volume report (BR-RPT-001)

## Technical Notes
- Endpoint: `GET /reports/sla`
- Entity: `Ticket`, `TicketSla`
- Business rules: BR-RPT-006, BR-RPT-007
- Spec: `specs/api/reports.md`

## Dependencies
- US-BE-039, US-BE-073
