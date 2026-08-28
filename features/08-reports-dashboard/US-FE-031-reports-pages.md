# US-FE-031 — Reports Pages (Ticket, SLA, Agents, CSAT)

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
**As a** manager, **I want to** view detailed reports with charts and tables, **so that** I can analyse team and service performance.

## Acceptance Criteria
- [ ] Route: `/reports/tickets` — summary cards + bar chart (trend by day/week/month) + byStatus/Priority/Channel donut charts + data table
- [ ] Route: `/reports/sla` — compliance gauges + byPriority breakdown table + breach reasons bar chart
- [ ] Route: `/reports/agents` (Admin/Manager only) — sortable table per agent with all KPIs
- [ ] Route: `/reports/csat` — avg rating gauge, distribution bar chart, byDepartment/Agent table, recent comments list
- [ ] Shared: date range picker (dateFrom/dateTo), department filter, "Export" button (CSV/Excel/PDF)
- [ ] Loading skeleton while fetching; error state with "Retry" button

## Technical Notes
- Components: `TicketReportComponent`, `SlaReportComponent`, `AgentReportComponent`, `CsatReportComponent`
- Charts: Angular-compatible chart library (e.g., `ngx-charts` or `Chart.js` via wrapper)
- Services: `ReportService.getTickets()`, `ReportService.getSla()`, `ReportService.getAgents()`, `ReportService.getCsat()`
- Spec: `specs/api/reports.md`

## Dependencies
- US-BE-073, US-BE-074, US-BE-075, US-BE-076, US-FE-005
