# US-BE-039 — SLA Clock Start on Ticket Creation

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
**Roles:** System
**As the** system, **I want to** start the SLA clock the moment a ticket is created, **so that** response and resolution deadlines are tracked from day one.

## Acceptance Criteria
- [ ] On `TicketCreated` event: look up the most specific `SlaPolicy` (department + priority, then global + priority)
- [ ] Snapshot SLA policy values onto the `TicketSla` record (not a FK — values copied)
- [ ] Calculate `FirstResponseDeadlineUtc` = `addBusinessMinutes(ticket.CreatedAt, policy.FirstResponseMinutes, businessHours)`
- [ ] Calculate `ResolutionDeadlineUtc` = `addBusinessMinutes(ticket.CreatedAt, policy.ResolutionMinutes, businessHours)`
- [ ] `TicketSla` record created atomically with the ticket
- [ ] Business hours used: department-specific override if exists, else global

## Technical Notes
- Implementation: `StartSlaClockCommandHandler`, triggered by `TicketCreated` domain event
- Entity: `TicketSla`, `SlaPolicy`, `BusinessHours`
- Business rules: BR-SLA-006, BR-SLA-007, BR-SLA-009
- Algorithm: `addBusinessMinutes` in `specs/features/03-sla-management.md`

## Dependencies
- US-BE-019, US-BE-043, US-BE-044
