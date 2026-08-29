# US-BE-021 — Get Ticket by ID

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
**Roles:** Admin, Manager, Agent, Customer
**As a** user, **I want to** view the full details of a ticket, **so that** I can understand its current state and history summary.

## Acceptance Criteria
- [ ] `GET /tickets/{id}` (agent+) returns full ticket object including `customer`, `assignedAgent`, `category`, `sla`, `customFieldValues`, `messagesCount`, `subjectAr`, `descriptionAr`
- [ ] `GET /portal/tickets/{id}` (customer) returns the same but: no internal agent UUID, no internal notes count, no custom field raw data — only customer-safe fields
- [ ] Customer accessing another customer's ticket returns `403`
- [ ] Agent accessing a ticket outside their departments returns `403` (unless Admin/Manager)
- [ ] Non-existent ticket returns `404`

## Technical Notes
- Endpoints: `GET /tickets/{id}`, `GET /portal/tickets/{id}`
- Entity: `Ticket` with related aggregates
- Business rule: BR-PLT-013
- Spec: `specs/api/tickets.md`, `specs/api/customer-portal.md`

## Dependencies
- US-BE-019, US-BE-007
