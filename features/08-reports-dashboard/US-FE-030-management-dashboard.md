# US-FE-030 — Management Dashboard (Live KPIs)

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
**As a** manager, **I want to** see live operational KPIs on a dashboard, **so that** I can monitor team performance in real time.

## Acceptance Criteria
- [ ] Route: `/reports/dashboard`
- [ ] KPI cards: Open Tickets, SLA Breach Rate, Avg First Response, Avg Resolution, CSAT Score, Agent Utilization, Unassigned Tickets, Escalation Rate
- [ ] Agent Workload table: per agent — open ticket count, availability status; colour-coded rows
- [ ] "Today" summary: Created vs Resolved bar
- [ ] Real-time: all cards update via SignalR `KpiUpdated` push without page refresh
- [ ] Department filter (Admin only); Manager sees their department only
- [ ] "Refresh" button for manual re-fetch

## Technical Notes
- Component: `ManagementDashboardComponent`
- Services: `DashboardService.getKpis()`
- SignalR: subscribes to `KpiUpdated` and `AgentWorkloadUpdated` on `DashboardHub`
- Spec: `specs/api/reports.md`

## Dependencies
- US-BE-077, US-BE-078, US-FE-005
