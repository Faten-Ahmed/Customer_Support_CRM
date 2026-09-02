# US-BE-042 — SLA Deadline Recalculation on Transfer

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
**Roles:** System (triggered by ticket transfer)
**As the** system, **I want to** recalculate SLA deadlines when a ticket is transferred to a different department, **so that** the new department's policy applies from the point of transfer.

## Acceptance Criteria
- [ ] On `TicketTransferred` event: load SLA policy for new department + ticket's current priority
- [ ] Compute already-elapsed business minutes (using transfer time as reference)
- [ ] New `FirstResponseDeadlineUtc` = `addBusinessMinutes(now, newPolicy.FirstResponseMinutes - elapsedMinutes, newDeptBusinessHours)`
- [ ] New `ResolutionDeadlineUtc` = `addBusinessMinutes(now, newPolicy.ResolutionMinutes - elapsedMinutes, newDeptBusinessHours)`
- [ ] If elapsed > new policy limit, deadline is set to `now` (already breached on arrival)
- [ ] Snapshot new SLA policy values onto `TicketSla`
- [ ] `TicketHistory` entry written noting SLA recalculation

## Technical Notes
- Implementation: handler on `TicketTransferred` event
- Entity: `TicketSla`, `SlaPolicy`
- Business rule: BR-SLA-007
- Spec: `specs/features/03-sla-management.md`

## Dependencies
- US-BE-026, US-BE-039
