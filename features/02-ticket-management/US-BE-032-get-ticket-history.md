# US-BE-032 — Get Ticket History

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
**As an** agent, **I want to** view a ticket's full audit history, **so that** I can see every change and who made it.

## Acceptance Criteria
- [ ] `GET /tickets/{id}/history` returns all `TicketHistory` entries sorted by `CreatedAt ASC`
- [ ] Each entry includes: `action`, `fromValue`, `toValue`, `note`, `actorName`, `actorRole`, `createdAt`
- [ ] No pagination limit — history is always returned in full (use client-side virtual scroll for large histories)
- [ ] History is immutable — no delete or update endpoint exists
- [ ] Customers cannot access this endpoint (`403`)

## Technical Notes
- Endpoint: `GET /tickets/{id}/history`
- Entity: `TicketHistory`
- Business rule: BR-TKT-013
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-019, US-BE-007
