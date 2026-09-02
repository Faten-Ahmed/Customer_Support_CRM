# US-BE-058 — Get My Assigned Tickets

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


**Epic:** Agent Dashboard
**Roles:** Agent
**As an** agent, **I want to** see only the tickets assigned to me, **so that** my work queue is focused.

## Acceptance Criteria
- [ ] `GET /agents/me/tickets` returns only tickets where `AssignedAgentId = caller.UserId`
- [ ] Default sort: `Priority DESC` then `ResolutionDeadlineUtc ASC` (most urgent first)
- [ ] Each item includes live SLA status: `slaStatus` (ok/warning/breach/criticalBreach), `resolutionRemainingMinutes`
- [ ] Filter: `?status=`, `?priority=`, `?categoryId=`, `?dateFrom=`, `?dateTo=`
- [ ] Paginated (default 20)
- [ ] Includes tickets across all departments the agent belongs to

## Technical Notes
- Endpoint: `GET /agents/me/tickets`
- Entity: `Ticket`, `TicketSla`
- Business rules: BR-AGT-001—005
- Spec: `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-019, US-BE-039, US-BE-007
