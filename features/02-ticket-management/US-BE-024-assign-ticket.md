# US-BE-024 — Assign Ticket to Agent

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
**Roles:** Admin, Manager, Agent (self-assign only)
**As a** manager, **I want to** assign a ticket to a specific agent, **so that** responsibility is clear.

## Acceptance Criteria
- [ ] `POST /tickets/{id}/assign` with `{ "agentId": "uuid" }` sets `AssignedAgentId`, `Status → Assigned`, `AssignedAt = now`
- [ ] Agent can only assign to themselves; assigning to another agent returns `403`
- [ ] Target agent must be active and in the ticket's department; otherwise `422`
- [ ] Concurrent self-assign on same unassigned ticket: first write wins; second returns `409` with code `TICKET_ALREADY_ASSIGNED`
- [ ] `TicketAssigned` domain event published → Notification sent to assigned agent
- [ ] `TicketHistory` entry written

## Technical Notes
- Endpoint: `POST /tickets/{id}/assign`
- Entity: `Ticket`, `TicketHistory`
- Business rule: BR-AGT-007
- Spec: `specs/api/tickets.md`, `specs/features/02-ticket-management.md`

## Dependencies
- US-BE-019, US-BE-007, US-BE-054
