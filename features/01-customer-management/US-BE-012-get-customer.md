# US-BE-012 — Get Customer by ID

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
**As an** agent, **I want to** view a customer's full profile, **so that** I understand their history before handling their ticket.

## Acceptance Criteria
- [ ] `GET /customers/{id}` returns `200` with full customer object including `contacts[]`, `isVip`, `companyName`, `ticketCount`
- [ ] Soft-deleted customer returns `404` (not exposed to agents)
- [ ] Customer not found returns `404`
- [ ] Response does NOT include `PasswordHash` or internal security fields

## Technical Notes
- Endpoint: `GET /customers/{id}`
- Entity: `Customer`, `CustomerContact`
- Spec: `specs/api/customers.md`

## Dependencies
- US-BE-009, US-BE-007
