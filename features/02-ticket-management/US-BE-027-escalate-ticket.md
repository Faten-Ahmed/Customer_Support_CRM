# US-BE-027 — Escalate Ticket

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
**As an** agent, **I want to** escalate a ticket, **so that** a manager is alerted and can intervene.

## Acceptance Criteria
- [ ] `POST /tickets/{id}/escalate` with `{ "reason": "..." }` sets `Status = Escalated`
- [ ] `reason` is required (min 20 chars); missing returns `422`
- [ ] `TicketEscalated` domain event published → notification sent to department Manager and Admin
- [ ] `TicketHistory` entry written with escalation reason
- [ ] Auto-escalation by SLA job (US-BE-041) uses reason `"SLA Critical Breach — auto-escalated"`

## Technical Notes
- Endpoint: `POST /tickets/{id}/escalate`
- Entity: `Ticket`, `TicketHistory`
- Business rule: BR-TKT-010
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-019, US-BE-007, US-BE-054
