# US-BE-026 — Transfer Ticket to Department

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
**As an** agent, **I want to** transfer a ticket to another department, **so that** the right team handles it.

## Acceptance Criteria
- [ ] `POST /tickets/{id}/transfer` with `{ "departmentId": "uuid", "transferNote": "..." }` changes `DepartmentId`, clears `AssignedAgentId`, sets `Status = New`
- [ ] `transferNote` is required (min 10 chars); missing returns `422`
- [ ] Target department must be active; inactive department returns `422`
- [ ] SLA deadlines are recalculated using the new department's policy and already-elapsed business minutes
- [ ] `TicketTransferred` domain event published; `TicketHistory` entry written
- [ ] Notifies agents in the new department

## Technical Notes
- Endpoint: `POST /tickets/{id}/transfer`
- Entity: `Ticket`, `TicketSla`, `TicketHistory`
- Business rule: BR-TKT-011, BR-SLA-007
- Spec: `specs/api/tickets.md`, `specs/features/02-ticket-management.md`

## Dependencies
- US-BE-019, US-BE-039, US-BE-007
