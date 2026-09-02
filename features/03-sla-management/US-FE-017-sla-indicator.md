# US-FE-017 — SLA Indicator Component

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


**Epic:** Ticket Management
**Roles:** Admin, Manager, Agent
**As an** agent, **I want to** see an at-a-glance SLA countdown on tickets, **so that** I prioritise the most urgent ones.

## Acceptance Criteria
- [ ] Compact badge used in ticket list: coloured dot (green/yellow/orange/red) + remaining time ("2h 15m")
- [ ] Detailed panel in ticket detail: shows both first response and resolution clocks with progress bars
- [ ] Progress bar fill: green (< 50%), yellow (50–80%), orange (80–100%), red (> 100%)
- [ ] Breach text: "⚠ Breached 30 min ago" in red when over 100%
- [ ] Paused state displayed when ticket is OnHold: "Paused" label shown
- [ ] Values refresh every 60 seconds on the detail page (polling or re-fetch)

## Technical Notes
- Component: `SlaIndicatorComponent` (shared, used in list + detail)
- Service: `TicketService.getSla()`
- Spec: `specs/api/tickets.md` — GET /tickets/{id}/sla

## Dependencies
- US-BE-033, US-FE-010
