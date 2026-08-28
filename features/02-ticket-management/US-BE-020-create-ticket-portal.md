# US-BE-020 — Create Ticket (Portal)

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
**Roles:** Customer
**As a** customer, **I want to** submit a support ticket via the portal, **so that** I can get help without calling or emailing.

## Acceptance Criteria
- [ ] `POST /portal/tickets` with `departmentId`, `subject`, `description` (required) returns `201`
- [ ] `priority` is always set to `Medium` by the system — customer cannot set priority
- [ ] `Channel` is always set to `Portal`
- [ ] `customerId` is inferred from the authenticated customer's JWT (not a request field)
- [ ] Required custom fields for the department must be provided; missing fields return `422`
- [ ] BR-TKT-004 enforced: if customer already has an open ticket in the same department and `forceNew` is not true, returns `409` with code `OPEN_TICKET_EXISTS` and the existing ticket ID

## Technical Notes
- Endpoint: `POST /portal/tickets`
- Entity: `Ticket`
- Business rules: BR-TKT-003, BR-TKT-004, BR-TKT-007 (portal always Medium)
- Spec: `specs/api/customer-portal.md`, `specs/features/09-customer-portal.md` BR-PLT-007—009

## Dependencies
- US-BE-019, US-BE-007
