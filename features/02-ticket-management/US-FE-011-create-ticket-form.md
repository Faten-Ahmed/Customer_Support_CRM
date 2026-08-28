# US-FE-011 — Create Ticket Form (Internal)

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
**As an** agent, **I want to** open a new ticket for a customer, **so that** their issue is tracked from the first contact.

## Acceptance Criteria
- [ ] Route: `/tickets/new` or dialog
- [ ] Fields: customer (searchable autocomplete), department (dropdown), category (hierarchical dropdown — parent then child), subject, description, priority, custom fields (dynamically loaded per selected department)
- [ ] Custom fields: rendered dynamically based on `GET /admin/field-definitions?departmentId=X`; required fields marked with asterisk
- [ ] On `departmentId` change: reload custom fields; clear previous custom field values
- [ ] On success: navigates to new ticket's detail page

## Technical Notes
- Component: `CreateTicketFormComponent`
- Services: `TicketService.create()`, `FieldDefinitionService.list()`, `CategoryService.tree()`
- Spec: `specs/api/tickets.md`, `specs/features/02-ticket-management.md`

## Dependencies
- US-BE-019, US-BE-070, US-FE-010
