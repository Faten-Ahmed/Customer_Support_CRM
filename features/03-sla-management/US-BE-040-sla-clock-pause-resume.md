# US-BE-040 — SLA Clock Pause / Resume

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


**Epic:** SLA Management
**Roles:** System (triggered by ticket status changes)
**As the** system, **I want to** pause the SLA clock when a ticket goes OnHold and resume it when work restarts, **so that** wait time outside the agent's control doesn't count against SLA.

## Acceptance Criteria
- [ ] On `TicketStatusChanged` to `OnHold`: set `TicketSla.PausedAt = now`
- [ ] On `TicketStatusChanged` from `OnHold` to `InProgress`: compute pause duration, add to `TicketSla.TotalPausedMinutes`, clear `PausedAt`
- [ ] Both SLA clocks (first response + resolution) pause/resume together
- [ ] Multiple OnHold cycles accumulate correctly in `TotalPausedMinutes`
- [ ] `GET /tickets/{id}/sla` always factors `TotalPausedMinutes` into elapsed calculation

## Technical Notes
- Implementation: handler on `TicketStatusChanged` event
- Entity: `TicketSla` (`PausedAt`, `TotalPausedMinutes`)
- Business rule: BR-SLA-006
- Spec: `specs/features/03-sla-management.md`

## Dependencies
- US-BE-039, US-BE-025
