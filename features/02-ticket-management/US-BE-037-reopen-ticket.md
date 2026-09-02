# US-BE-037 — Reopen Ticket (Customer Reply)

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
**Roles:** Customer (triggered automatically), Agent
**As the** system, **I want to** reopen a Resolved ticket when a customer replies, **so that** the agent knows the issue is not fixed.

## Acceptance Criteria
- [ ] When `POST /portal/tickets/{id}/messages` is called on a ticket with `Status = Resolved`, the ticket is transitioned to `InProgress` (via the reopen flow)
- [ ] `TicketHistory` entry written with action `Reopened`
- [ ] Re-assignment: if `AssignedAgentId` agent is still active in the department → keep them; otherwise re-enter auto-assign flow (US-BE-035)
- [ ] SLA resolution deadline is recalculated (remaining time reset per policy)
- [ ] `TicketStatusChanged` and `TicketReopened` events published → agent notified
- [ ] The created `TicketMessage` (the customer's reply) is returned as the `201` response body

## Technical Notes
- Triggered by: `POST /portal/tickets/{id}/messages` when `Ticket.Status == Resolved`
- Entity: `Ticket`, `TicketHistory`, `TicketSla`
- Business rule: BR-TKT-012 (cannot reply to Closed), W-TKT-04
- Spec: `specs/features/02-ticket-management.md`, `specs/features/09-customer-portal.md` BR-PLT-016

## Dependencies
- US-BE-028, US-BE-025, US-BE-035, US-BE-039
