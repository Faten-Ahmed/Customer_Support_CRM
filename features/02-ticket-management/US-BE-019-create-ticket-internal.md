# US-BE-019 — Create Ticket (Internal)

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
**As an** agent, **I want to** open a ticket on behalf of a customer, **so that** their issue is tracked in the system.

## Acceptance Criteria
- [ ] `POST /tickets` with `customerId`, `departmentId`, `subject`, `description` (required) returns `201` with `id`, `ticketNumber`, `status = New`, `createdAt`
- [ ] `priority` defaults to `Medium` if not provided; any priority allowed when set by agent
- [ ] `Channel` is set by the system based on context (default `Portal` when created via API without channel; actual channel set by inbound handlers)
- [ ] `categoryId`, `customFieldValues` are optional; if provided, validated against active field definitions (US-BE-038)
- [ ] `subjectAr` (Arabic subject) and `descriptionAr` (Arabic description) are optional bilingual fields; stored as-is alongside primary fields
- [ ] `TicketNumber` generated as `TKT-{YEAR}-{PADDED_SEQ}` (sequence lock + increment)
- [ ] `TicketCreated` domain event published → triggers SLA start (US-BE-039) and auto-assign (US-BE-035)
- [ ] Unknown `customerId` or `departmentId` returns `404`

## Technical Notes
- Endpoint: `POST /tickets`
- Entity: `Ticket`, `TicketMessage` (initial message = description)
- Business rules: BR-TKT-001, BR-TKT-002, BR-TKT-003, BR-TKT-004
- Spec: `specs/api/tickets.md`, `specs/features/02-ticket-management.md` W-TKT-01

## Dependencies
- US-BE-007, US-BE-009, US-BE-038
