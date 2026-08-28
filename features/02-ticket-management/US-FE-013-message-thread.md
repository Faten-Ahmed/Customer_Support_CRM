# US-FE-013 — Message Thread Component

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
**As an** agent, **I want to** read the full conversation thread in a clear, chat-like layout, **so that** I can understand the ticket's context at a glance.

## Acceptance Criteria
- [ ] Messages displayed chronologically (oldest at top); auto-scrolls to bottom on load
- [ ] Agent messages: right-aligned, blue background
- [ ] Customer messages: left-aligned, grey background
- [ ] Internal notes: full-width yellow background with "Internal Note" label
- [ ] Each message shows: sender name, sender role, timestamp (relative + absolute on hover), delivery status badge (Sent/Failed/Pending) for outbound
- [ ] Paginated (load more button at top for older messages)
- [ ] Real-time: new incoming messages appended without full page reload (SignalR)

## Technical Notes
- Component: `MessageThreadComponent`
- Service: `TicketService.getMessages()`
- SignalR: subscribe to `ReceiveMessage` on ticket's channel
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-029, US-FE-010
