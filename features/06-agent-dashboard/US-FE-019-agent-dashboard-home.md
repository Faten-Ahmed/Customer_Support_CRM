# US-FE-019 — Agent Dashboard Home (My Tickets)

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


**Epic:** Agent Dashboard
**Roles:** Agent
**As an** agent, **I want to** see my personalised dashboard when I log in, **so that** I immediately know what to work on.

## Acceptance Criteria
- [ ] Route: `/dashboard` (default landing page after login)
- [ ] Summary cards: Open Tickets, SLA Breached, OnHold, Resolved Today
- [ ] My Tickets table: same as ticket list but pre-filtered to `AssignedAgentId = me`; default sort by SLA urgency
- [ ] Quick links: "Unassigned Queue", "New Ticket"
- [ ] Availability status toggle in top-right corner (Available/Busy/Away/Offline)
- [ ] Notification bell with unread badge
- [ ] Real-time: SLA badge updates in table every 60s

## Technical Notes
- Component: `AgentDashboardComponent`
- Services: `TicketService.getMyTickets()`, `AgentService.updateAvailability()`
- Spec: `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-058, US-BE-059, US-FE-005
