# US-BE-036 — Auto-Close Resolved Tickets Job

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
**Roles:** System (Hangfire recurring job)
**As the** system, **I want to** automatically close tickets that have been resolved for 48+ hours with no customer reply, **so that** agents don't need to manually close every ticket.

## Acceptance Criteria
- [ ] `AutoCloseResolvedTicketsJob` runs every 30 minutes (Hangfire cron: `*/30 * * * *`)
- [ ] Finds all tickets with `Status = Resolved` and `ResolvedAt < now - 48h` and no `TicketMessage` from a Customer since `ResolvedAt`
- [ ] For each match: sets `Status = Closed`, `ClosedAt = now`, `ClosedBy = "System"`
- [ ] `TicketClosed` domain event published → CSAT survey dispatched (US-BE-111)
- [ ] `TicketHistory` entry written with action `AutoClosed`, note `"No customer response within 48 hours"`

## Technical Notes
- Implementation: Hangfire recurring job registered in `Program.cs`
- Entity: `Ticket`, `TicketMessage`
- Business rule: W-TKT-05 in `specs/features/02-ticket-management.md`

## Dependencies
- US-BE-019, US-BE-028, US-BE-111
