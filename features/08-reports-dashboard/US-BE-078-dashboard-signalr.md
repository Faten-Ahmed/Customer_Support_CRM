# US-BE-078 — Real-Time Dashboard (SignalR DashboardHub)

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
**As a** manager, **I want** the dashboard KPIs to update automatically, **so that** I see live data without refreshing.

## Acceptance Criteria
- [ ] `DashboardHub` authenticates via JWT query param; adds connection to role-scoped group
- [ ] On `TicketCreated`, `TicketStatusChanged`, `CsatSubmitted`, `SlaBreached` events: recompute KPIs and push `KpiUpdated` to affected groups
- [ ] Push is debounced: if multiple events arrive within 2 seconds, only one push is sent (BR-RPT-013)
- [ ] On `TicketAssigned` or `AgentStatusChanged`: push `AgentWorkloadUpdated` with updated per-agent array
- [ ] Payload is the same shape as `GET /dashboard/kpis`

## Technical Notes
- Hub URL: `ws://localhost:5000/hubs/dashboard`
- Implementation: `DashboardHub`, `IDashboardPusher` service
- Business rules: BR-RPT-013, BR-RPT-014
- Spec: `specs/api/reports.md`

## Dependencies
- US-BE-077, US-BE-053
