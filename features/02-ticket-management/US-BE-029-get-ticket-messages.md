# US-BE-029 — Get Ticket Messages

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
**As an** agent, **I want to** read the full message thread on a ticket, **so that** I have context before replying.

## Acceptance Criteria
- [ ] `GET /tickets/{id}/messages` (agent+) returns all messages including internal notes; paginated
- [ ] `GET /portal/tickets/{id}/messages` (customer) returns only `isInternal = false` messages
- [ ] Each message includes: `id`, `senderName`, `senderType`, `content`, `isInternal`, `createdAt`, `deliveryStatus`
- [ ] Default sort: `createdAt ASC` (oldest first for threaded conversation)
- [ ] Customer accessing another customer's ticket messages returns `403`

## Technical Notes
- Endpoints: `GET /tickets/{id}/messages`, `GET /portal/tickets/{id}/messages`
- Entity: `TicketMessage`
- Business rule: BR-TKT-006, BR-PLT-013
- Spec: `specs/api/tickets.md`, `specs/api/customer-portal.md`

## Dependencies
- US-BE-028
