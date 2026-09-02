# US-FE-010 — Ticket Detail Page Shell

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
**As an** agent, **I want to** view all ticket information and take actions from a single page, **so that** I don't need to navigate away while handling a ticket.

## Acceptance Criteria
- [ ] Route: `/tickets/{id}`
- [ ] Left panel: ticket metadata (status, priority, department, category, SLA indicator, assigned agent, customer info, custom fields)
- [ ] Centre panel: message thread (US-FE-014) + reply composer (US-FE-015)
- [ ] Right panel: AI panel (summarize, suggest reply, suggest articles, suggest category)
- [ ] Action bar (top): Assign, Transfer, Escalate, Close, Change Status buttons (role/status-gated)
- [ ] Tabs: Messages, History, Attachments
- [ ] Real-time: incoming messages pushed via SignalR (if agent is viewing and a customer replies, message appears without refresh)

## Technical Notes
- Component: `TicketDetailComponent`
- Service: `TicketService.getById()`
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-021, US-FE-009
