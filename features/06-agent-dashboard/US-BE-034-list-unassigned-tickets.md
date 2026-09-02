# US-BE-034 — List Unassigned Tickets

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
**As an** agent, **I want to** see the queue of unassigned tickets in my departments, **so that** I can pull the next ticket to work on.

## Acceptance Criteria
- [ ] `GET /tickets/unassigned` returns tickets with `Status = New` and `AssignedAgentId IS NULL`
- [ ] Agent scope: filtered to departments the calling agent belongs to
- [ ] Sorted by `CreatedAt ASC` (oldest first)
- [ ] Includes SLA urgency fields (`resolutionDeadlineUtc`, `currentBreachLevel`) per ticket
- [ ] Paginated; default `pageSize = 20`

## Technical Notes
- Endpoint: `GET /tickets/unassigned`
- Entity: `Ticket`, `TicketSla`
- Business rule: BR-AGT-006
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-019, US-BE-007
