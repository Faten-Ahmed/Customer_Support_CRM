# US-BE-041 — SLA Monitoring Job (Breach Detection)

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
**Roles:** System (Hangfire recurring job)
**As the** system, **I want to** periodically check all open tickets' SLA progress and fire alerts at the right thresholds, **so that** agents and managers are warned before and when a breach occurs.

## Acceptance Criteria
- [ ] `SlaMonitorJob` runs every 5 minutes (Hangfire cron: `*/5 * * * *`)
- [ ] Queries all open tickets (Status not in `Closed`, `Resolved`) that have not had their first response and are not `OnHold`
- [ ] For each ticket, computes current `percentElapsed` for both clocks
- [ ] If `percentElapsed ≥ 80` and `FirstResponseBreachLevel IS NULL` → set `Warning`, create notification for assigned agent
- [ ] If `percentElapsed ≥ 100` and `FirstResponseBreachLevel < Breach` → set `Breach`, notify agent + manager
- [ ] If `percentElapsed ≥ 200` and `FirstResponseBreachLevel < CriticalBreach` → set `CriticalBreach`, notify agent + manager + admin; auto-escalate ticket (US-BE-027)
- [ ] Duplicate suppression: skip if notification of same tier + ticketId already exists (BR-SLA-008)
- [ ] Same logic applied to resolution clock independently

## Technical Notes
- Implementation: Hangfire recurring job
- Entity: `TicketSla`, `Notification`, `Ticket`
- Business rules: all breach tier rules in `specs/features/03-sla-management.md`

## Dependencies
- US-BE-039, US-BE-040, US-BE-054, US-BE-027
