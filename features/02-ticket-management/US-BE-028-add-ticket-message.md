# US-BE-028 — Add Ticket Message

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
**As an** agent, **I want to** post a reply or internal note on a ticket, **so that** communication is threaded and auditable.

## Acceptance Criteria
- [ ] `POST /tickets/{id}/messages` with `{ "content": "...", "isInternal": false }` creates a `TicketMessage`; returns `201`
- [ ] `isInternal = true` is only allowed for Agent+ role; Customer posting with `isInternal = true` returns `403`
- [ ] Posting to a Closed ticket returns `422` with code `TICKET_CLOSED`
- [ ] Customer posting to a Resolved ticket triggers reopen flow (US-BE-037)
- [ ] If `isInternal = false` and sender is Agent: enqueues outbound send job for the ticket's channel (US-BE-105—107)
- [ ] `TicketMessageAdded` domain event published → notifications dispatched

## Technical Notes
- Endpoint: `POST /tickets/{id}/messages`
- Entity: `TicketMessage`
- Business rules: BR-TKT-006, BR-TKT-012
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-019, US-BE-007, US-BE-054
