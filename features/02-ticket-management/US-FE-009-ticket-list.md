# US-FE-009 — Ticket List Page (Agent)

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
**As an** agent, **I want to** see all relevant tickets with rich filters, **so that** I can manage my workload effectively.

## Acceptance Criteria
- [ ] Route: `/tickets`
- [ ] Table with columns: Ticket #, Subject, Customer, Status badge, Priority badge, SLA indicator, Assigned Agent, Department, Created date
- [ ] Filter sidebar: status (multi-select), priority, department, category, assigned agent, date range
- [ ] Search bar: by ticket number or subject
- [ ] Default sort: Priority DESC, SLA urgency ASC
- [ ] SLA indicator: green/yellow/red dot per urgency
- [ ] "New Ticket" button → opens ticket creation form
- [ ] Row click → navigates to `/tickets/{id}`
- [ ] Pagination with total count

## Technical Notes
- Component: `TicketListComponent` in `TicketsModule`
- Service: `TicketService.list()`
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-022, US-FE-005
