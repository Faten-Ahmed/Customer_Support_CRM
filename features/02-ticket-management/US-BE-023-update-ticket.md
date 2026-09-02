# US-BE-023 — Update Ticket

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
**As an** agent, **I want to** update a ticket's subject, category, priority, and custom fields, **so that** the ticket metadata is accurate.

## Acceptance Criteria
- [ ] `PUT /tickets/{id}` accepts partial updates: `subject`, `subjectAr`, `descriptionAr`, `categoryId`, `priority`, `customFieldValues`
- [ ] Returns `200` with the updated ticket
- [ ] Changing `priority` recalculates SLA deadlines from the current moment (elapsed time preserved)
- [ ] Changing `categoryId` publishes `TicketCategoryChanged` history entry
- [ ] `TicketHistory` entry written for each changed field
- [ ] Closed ticket cannot be updated (returns `422` with code `TICKET_CLOSED`)

## Technical Notes
- Endpoint: `PUT /tickets/{id}`
- Entity: `Ticket`, `TicketHistory`
- Business rule: BR-TKT-012
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-019, US-BE-038, US-BE-007
