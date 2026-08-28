# US-BE-033 — Get Ticket SLA Details

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
**As an** agent, **I want to** view a ticket's SLA status in real time, **so that** I know how much time I have left to respond and resolve.

## Acceptance Criteria
- [ ] `GET /tickets/{id}/sla` returns: `firstResponseDeadlineUtc`, `resolutionDeadlineUtc`, `firstRespondedAt`, `resolvedAt`, `elapsedBusinessMinutes`, `remainingBusinessMinutes`, `percentElapsed`, `currentBreachLevel` for both clocks
- [ ] `percentElapsed` and `remainingBusinessMinutes` are computed fresh on each call (not stale from the last monitoring job run)
- [ ] `currentBreachLevel` can show `Warning` even if the DB field hasn't been updated yet by the monitoring job
- [ ] Returns `404` if ticket not found; `403` for customer role

## Technical Notes
- Endpoint: `GET /tickets/{id}/sla`
- Entity: `TicketSla`
- Business rule: all of `specs/features/03-sla-management.md`
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-039, US-BE-019
