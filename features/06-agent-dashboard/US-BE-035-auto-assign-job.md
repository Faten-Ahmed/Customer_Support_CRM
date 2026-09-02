# US-BE-035 — Auto-Assign Ticket Job

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
**Roles:** System (Hangfire job)
**As the** system, **I want to** automatically assign new tickets to the best-fit available agent, **so that** tickets don't sit unassigned.

## Acceptance Criteria
- [ ] `AutoAssignTicketJob` triggered by `TicketCreated` event
- [ ] Step 1 — Skills match: finds Agents in the ticket's Department whose skills include the ticket's `CategoryId`, picks the one with fewest open tickets
- [ ] Step 2 — Round-robin: if no skills match, picks the active Agent in the department with the oldest `LastAssignedAt`
- [ ] Step 3 — Pull queue: if all agents have > 15 open tickets, ticket stays `New`; Manager notified with `UnassignedTicketAlert`
- [ ] Step 4 — No active agents: if all agents are Offline/Away, Manager notified with `UnassignedTicketAlert`
- [ ] On assignment: `Ticket.AssignedAgentId` set, `Status = Assigned`, `AssignedAt = now`, `LastAssignedAt` updated on agent
- [ ] `TicketAssigned` event published; `TicketHistory` entry written

## Technical Notes
- Implementation: Hangfire background job, triggered via `IBackgroundJobClient.Enqueue<AutoAssignTicketJob>`
- Entity: `Ticket`, `User` (AvailabilityStatus, AgentDepartment, AgentSkill)
- Business rules: W-TKT-02 in `specs/features/02-ticket-management.md`

## Dependencies
- US-BE-019, US-BE-054, US-BE-007
