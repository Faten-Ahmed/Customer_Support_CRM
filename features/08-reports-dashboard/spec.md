# Feature Spec — Reports & Management Dashboard

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


> Requirements: `REQ-RPT-*`
> API: `specs/api/reports.md`
> Domain entities: `Ticket`, `TicketSla`, `CsatSurvey`, `User`
> Real-time: SignalR `DashboardHub`

---

## Overview

The Reports module provides two surfaces: historical reports (date-range queries) for trend analysis and performance review, and a live Management Dashboard (real-time KPI snapshot). Reports are scoped by the caller's role. Export to CSV/Excel/PDF is available for Admin and Manager.

---

## Role-Based Scoping

**BR-RPT-001** Report data is scoped by role:
- `Admin`: sees all data across all departments and agents.
- `Manager`: sees data for their primary department only. Cannot see other departments' data.
- `Agent`: sees data for their own tickets only (no cross-agent view). Can access ticket volume and SLA reports filtered to their work.

**BR-RPT-002** Query params `departmentId` and `agentId` are respected within the caller's scope. An Agent passing `agentId` for another agent returns `403`. A Manager passing `departmentId` for a department they don't manage returns `403`.

---

## Report Definitions

### Ticket Volume Report (`GET /reports/tickets`)

Computes:
- **Summary**: `totalCreated`, `totalResolved`, `totalClosed`, `openAtEndOfPeriod`
- **By Status**: count of tickets in each status at end of period
- **By Priority**: count of tickets per priority created in the period
- **By Channel**: count of tickets per channel created in the period
- **Trend**: time series of `created` and `resolved` counts grouped by `day`, `week`, or `month`

**BR-RPT-003** `dateFrom` and `dateTo` are inclusive, interpreted as UTC dates (00:00:00 to 23:59:59 UTC). Both are required; date range max 365 days.

**BR-RPT-004** `openAtEndOfPeriod` = count of tickets that were created on or before `dateTo` and whose `ClosedAt` is after `dateTo` or null.

**BR-RPT-005** Trend data: when `groupBy = week`, weeks start on Sunday (configurable to Monday in v2). Partial weeks at range boundaries are included as-is.

---

### SLA Compliance Report (`GET /reports/sla`)

Computes:
- **First Response Compliance**: `total` tickets evaluated, `met`, `breached`, `complianceRate` (%)
- **Resolution Compliance**: same metrics for resolution SLA
- **By Priority**: breakdown of avg response/resolution times and compliance rates per priority
- **Breach Reasons**: counts of Warning / Breach / CriticalBreach events in period

**BR-RPT-006** A ticket is counted in SLA compliance only if it was created within the date range. Tickets created before `dateFrom` but resolved within range are excluded (their SLA measurement started outside the window).

**BR-RPT-007** `avgFirstResponseMinutes` and `avgResolutionMinutes` are computed in business minutes (same algorithm as SLA clock). Raw wall-clock times are not reported.

---

### Agent Performance Report (`GET /reports/agents`)

**Auth:** Admin and Manager only.

Computes per agent:
- `ticketsHandled`: tickets assigned to the agent in the period
- `ticketsResolved`: tickets resolved by the agent in the period
- `avgFirstResponseMinutes`: average across their tickets (business minutes)
- `avgResolutionMinutes`: average resolution time (business minutes)
- `slaComplianceRate`: % of their tickets that met both SLA clocks
- `csatScore`: average CSAT rating on surveys submitted for their resolved tickets
- `csatResponseCount`: number of CSAT responses received
- `escalationRate`: % of their tickets that were escalated (outgoing escalations only — not tickets transferred to them already escalated)

**BR-RPT-008** Agents with zero tickets in the period are excluded from the report (not shown as zero-rows).

**BR-RPT-009** `csatScore` is null (not 0) if `csatResponseCount = 0`.

---

### CSAT Report (`GET /reports/csat`)

Computes:
- **Overall**: `avgRating`, `totalSent`, `totalSubmitted`, `responseRate` (%)
- **Distribution**: count per rating value (1–5)
- **By Department**: avg rating + response count per department
- **By Agent**: avg rating + response count per agent
- **Recent Comments**: last 20 submitted comments with ticket reference

**BR-RPT-010** Only `Status = Submitted` surveys are counted in averages. Sent-but-not-submitted surveys count toward `totalSent` only.

**BR-RPT-011** Expired surveys (past 7-day window) where `Status = Sent` count toward `totalSent` but not `totalSubmitted`. They are excluded from `avgRating`.

---

## Management Dashboard (`GET /dashboard/kpis`)

A real-time snapshot of the current operational state. Data is computed at query time (not pre-aggregated) for accuracy.

Metrics:
- `openTickets`: total open tickets (Status not in Closed) + breakdown by priority
- `slaBreachRate`: % of currently open tickets that have breached any SLA clock
- `avgFirstResponseMinutes`: rolling 7-day average across all departments in scope
- `avgResolutionMinutes`: rolling 7-day average
- `csatScore`: rolling 30-day average CSAT score
- `agentUtilization`: % of active agents who have at least 1 open ticket assigned
- `ticketsTodayCreated`: count of tickets created today (UTC)
- `ticketsTodayResolved`: count of tickets resolved today (UTC)
- `escalationRate`: % of open tickets currently in Escalated status
- `unassignedTickets`: count of Status=New tickets with no AssignedAgentId
- `agentWorkload`: per-agent open ticket count + current availability status

**BR-RPT-012** `GET /dashboard/kpis` without `departmentId` returns organization-wide metrics for Admin. Manager calling without `departmentId` sees their own department. Agent calling this endpoint sees their personal KPIs only (no org-wide data).

---

## Real-Time Dashboard Updates (SignalR DashboardHub)

**Hub URL:** `ws://localhost:5000/hubs/dashboard?access_token=<token>`

**Server → Client methods:**

| Method | Payload | Trigger Event |
|--------|---------|--------------|
| `KpiUpdated` | Full KPI object (same as GET /dashboard/kpis) | `TicketCreated`, `TicketStatusChanged`, `CsatSubmitted`, `SlaBreached` |
| `AgentWorkloadUpdated` | `[{ agentId, openTickets, status }]` | `TicketAssigned`, `AgentStatusChanged` |

**BR-RPT-013** The `KpiUpdated` push is debounced: if multiple trigger events arrive within 2 seconds, only one push is sent (using the most recent computed values). This prevents dashboard flicker during bulk operations.

**BR-RPT-014** Dashboard Hub subscribers are scoped by role same as REST: Managers see only their department's KPIs in the push payload.

---

## Export (`GET /reports/export`)

**Auth:** Admin and Manager only.

**BR-RPT-015** Supported formats: `csv`, `xlsx`, `pdf`. Content-Type and Content-Disposition headers are set appropriately.

**BR-RPT-016** Export is synchronous for datasets under 10,000 rows. For larger datasets, the API returns `202 Accepted` with a job ID, and the file is generated asynchronously by Hangfire. The client polls `GET /reports/export/status/{jobId}` until complete, then downloads from `GET /reports/export/download/{jobId}` (pre-signed S3 URL, 15-minute TTL). *(Polling endpoint not yet in API spec — to be added in implementation.)*

**BR-RPT-017** PDF exports include: company logo, report title, date range, generated-at timestamp, and a summary table. Chart images are not included in v1 PDF (tables only).

**BR-RPT-018** Exported reports apply the same role-based scoping as their JSON counterparts.

---

## Acceptance Criteria

**AC-RPT-001** Given a Manager from Department A calls `GET /reports/agents?departmentId=<Department B UUID>`, then the response is `403 Forbidden`.

**AC-RPT-002** Given a ticket was created on Oct 1 and closed on Oct 20, and the report range is Oct 10–Oct 31, then the ticket is NOT counted in ticket volume (created outside range) but IS counted in `openAtEndOfPeriod` only if it was still open at end of Oct 31 (it was closed Oct 20, so it is not in openAtEndOfPeriod either).

**AC-RPT-003** Given `GET /reports/tickets?dateFrom=2025-01-01&dateTo=2026-01-02`, then the response is `422` (date range exceeds 365 days).

**AC-RPT-004** Given Agent A has no CSAT responses in the period, when their row appears in the agent performance report, then `csatScore = null` (not 0).

**AC-RPT-005** Given a TicketCreated and TicketStatusChanged event arrive within 1 second of each other, when DashboardHub pushes updates, then only one `KpiUpdated` message is sent to the dashboard (debounce).

**AC-RPT-006** Given `GET /reports/export?format=xlsx&reportType=tickets`, when the dataset is 500 rows, then the response is `200` with an xlsx file attachment (not deferred).

**AC-RPT-007** Given `GET /dashboard/kpis` called by an Agent, then only their personal metrics are returned (no org-wide `openTickets`, no `agentWorkload` array).

---

## Edge Cases

- **Empty date range results**: if no tickets exist in the range, all numeric fields are 0 (not null), trend array is empty `[]`. `csatScore` is null (undefined average with zero data points).
- **Manager with no tickets in period**: agent performance report returns empty `data: []` array, not an error.
- **Agent utilization calculation**: `(agents with ≥ 1 open ticket) / (total Available + Busy agents)`. Offline and Away agents are excluded from denominator to avoid penalizing departments for agents who are off-shift.
- **Timezone in exports**: all timestamps in exports are shown in the report requester's configured timezone (stored in their user profile). Default: `Asia/Riyadh`.

---

## Integration Points

Reports are read-only queries — they publish no domain events. They consume data from:
- `Ticket`, `TicketSla`, `TicketMessage` (response time)
- `CsatSurvey`, `CsatResponse`
- `User`, `Department`
- SignalR DashboardHub is triggered by domain events from Ticket and CSAT modules.
