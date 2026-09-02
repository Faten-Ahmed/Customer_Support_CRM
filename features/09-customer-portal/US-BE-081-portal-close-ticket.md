# US-BE-081 — Portal Close Ticket

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


**Epic:** Customer Portal
**Roles:** Customer
**As a** customer, **I want to** close my own ticket when my issue is resolved, **so that** I don't have to wait for an agent to close it.

## Acceptance Criteria
- [ ] `POST /portal/tickets/{id}/close` transitions any open status → `Closed`; returns `{ "id", "status": "Closed" }`
- [ ] Ticket already Closed returns `422` with code `TICKET_ALREADY_CLOSED`
- [ ] Customer accessing another customer's ticket returns `403`
- [ ] `TicketClosed` domain event published → CSAT survey dispatched (US-BE-111)
- [ ] `TicketHistory` entry written with `closedBy = Customer`

## Technical Notes
- Endpoint: `POST /portal/tickets/{id}/close`
- Entity: `Ticket`, `TicketHistory`
- Business rules: BR-PLT-018, BR-PLT-019
- Spec: `specs/api/customer-portal.md`

## Dependencies
- US-BE-020, US-BE-111, US-BE-007
