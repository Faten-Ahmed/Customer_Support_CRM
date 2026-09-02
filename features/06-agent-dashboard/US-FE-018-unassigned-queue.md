# US-FE-018 — Unassigned Ticket Queue Page

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
**As an** agent, **I want to** see all unassigned tickets in my departments and claim one, **so that** I can start working on tickets proactively.

## Acceptance Criteria
- [ ] Route: `/tickets/unassigned`
- [ ] List view: Ticket #, Subject, Customer, Department, Priority, SLA, Created (oldest first)
- [ ] "Claim" button per row: calls `POST /tickets/{id}/assign` with own agentId; on `409 TICKET_ALREADY_ASSIGNED` shows "Claimed by someone else — refreshing"
- [ ] Real-time: row disappears when another agent claims a ticket (SignalR `TicketAssigned` event)
- [ ] Empty state: "No unassigned tickets — great work!"

## Technical Notes
- Component: `UnassignedQueueComponent`
- Services: `TicketService.listUnassigned()`, `TicketService.assign()`
- SignalR: subscribe to `TicketAssigned` event on department group

## Dependencies
- US-BE-034, US-BE-024, US-FE-009
