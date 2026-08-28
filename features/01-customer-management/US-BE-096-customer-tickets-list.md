# US-BE-096 — Customer Tickets List (Internal)

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


**Epic:** Customer Management
**Roles:** Admin, Manager, Agent
**As an** agent, **I want to** view all tickets for a specific customer, **so that** I understand their history before handling a new request.

## Acceptance Criteria
- [ ] `GET /customers/{id}/tickets` returns paginated list of tickets for that customer
- [ ] Agent scope: only tickets in departments the agent belongs to; Admin/Manager: all tickets
- [ ] Filter: `?status=`, `?page=`, `?pageSize=`; default `pageSize = 20`
- [ ] Each item includes: `ticketNumber`, `subject`, `status`, `priority`, `createdAt`, `category`
- [ ] Returns `404` if customer not found or is soft-deleted

## Technical Notes
- Endpoint: `GET /customers/{id}/tickets`
- Entity: `Ticket`
- Business rule: BR-CUST-011
- Spec: `specs/api/customers.md`

## Dependencies
- US-BE-009, US-BE-019, US-BE-007
