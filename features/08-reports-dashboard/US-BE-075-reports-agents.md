# US-BE-075 — Agent Performance Report

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
**Roles:** Admin, Manager
**As a** manager, **I want to** see each agent's performance metrics, **so that** I can coach underperformers and recognise top performers.

## Acceptance Criteria
- [ ] `GET /reports/agents` returns per-agent: ticketsHandled, ticketsResolved, avgFirstResponseMinutes, avgResolutionMinutes, slaComplianceRate, csatScore, csatResponseCount, escalationRate
- [ ] Agent role returns `403`
- [ ] Agents with zero tickets in the period are excluded
- [ ] `csatScore = null` (not 0) when `csatResponseCount = 0`
- [ ] Scoping: Manager sees their department only; cross-department filter returns `403`

## Technical Notes
- Endpoint: `GET /reports/agents`
- Entity: `Ticket`, `TicketSla`, `CsatSurvey`, `User`
- Business rules: BR-RPT-008, BR-RPT-009
- Spec: `specs/api/reports.md`

## Dependencies
- US-BE-073, US-BE-007
