# US-BE-022 — List Tickets with Filters

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
**As an** agent, **I want to** list and filter tickets, **so that** I can find relevant tickets quickly.

## Acceptance Criteria
- [ ] `GET /tickets` returns paginated list; default `pageSize = 20`, max `50`
- [ ] Filters: `status`, `priority`, `departmentId`, `agentId`, `categoryId`, `customerId`, `dateFrom`, `dateTo`, `search` (subject/ticketNumber)
- [ ] Agent scope: sees only tickets in their departments (not all tickets)
- [ ] Manager scope: sees tickets in their primary department
- [ ] Admin scope: sees all tickets
- [ ] `GET /portal/tickets` scoped to authenticated customer's tickets only
- [ ] Sort: `?sortBy=createdAt|priority|sla&sortDir=asc|desc`

## Technical Notes
- Endpoints: `GET /tickets`, `GET /portal/tickets`
- Entity: `Ticket`
- Business rule: BR-RPT-001
- Spec: `specs/api/tickets.md`, `specs/api/customer-portal.md`

## Dependencies
- US-BE-019, US-BE-007
