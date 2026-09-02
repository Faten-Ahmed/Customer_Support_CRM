# US-BE-025 — Change Ticket Status

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
**As an** agent, **I want to** change a ticket's status (e.g., start working, put on hold, resolve), **so that** the ticket lifecycle is accurately tracked.

## Acceptance Criteria
- [ ] `POST /tickets/{id}/status` with `{ "status": "InProgress", "note": "..." }` transitions the ticket if the transition is valid per the state machine
- [ ] Invalid transitions return `422` with code `INVALID_STATUS_TRANSITION` listing allowed next states
- [ ] `Status → Resolved` requires `resolution` field (min 10 chars); missing returns `422`
- [ ] `Status → OnHold` triggers SLA clock pause
- [ ] `Status → InProgress` (from OnHold) resumes SLA clock
- [ ] `TicketStatusChanged` domain event published
- [ ] `TicketHistory` entry written with `fromStatus`, `toStatus`, `note`, `actorId`

## Technical Notes
- Endpoint: `POST /tickets/{id}/status`
- Entity: `Ticket`, `TicketHistory`, `TicketSla`
- Business rules: all status transition rules in `specs/features/02-ticket-management.md` (state machine table)
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-019, US-BE-039, US-BE-007
