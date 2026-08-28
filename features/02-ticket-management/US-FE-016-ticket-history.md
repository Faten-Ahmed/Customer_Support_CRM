# US-FE-016 — Ticket History Tab

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
**As an** agent, **I want to** see a chronological audit trail of all changes to a ticket, **so that** I can understand how the ticket evolved.

## Acceptance Criteria
- [ ] "History" tab in ticket detail shows all `TicketHistory` entries
- [ ] Each entry: action icon, action label (e.g., "Status changed: InProgress → OnHold"), actor name + role, timestamp
- [ ] Entries sorted oldest first; no pagination (full list shown)
- [ ] Different action types styled differently (status changes, assignments, escalations, transfers, messages, SLA events)
- [ ] Not visible to customers

## Technical Notes
- Component: `TicketHistoryComponent`
- Service: `TicketService.getHistory()`
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-032, US-FE-010
