# US-BE-061 — Render Template with Ticket Context

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
**Roles:** Agent, Manager, Admin
**As an** agent, **I want to** render a quick-reply template with a specific ticket's data substituted in, **so that** I get a ready-to-send personalised reply.

## Acceptance Criteria
- [ ] `POST /agents/me/templates/{id}/render` with `{ "ticketId": "uuid" }` returns rendered `content` string
- [ ] Substitutions: `{{customer_name}}` → ticket's customer fullName, `{{agent_name}}` → caller's fullName, `{{ticket_number}}` → ticket's TicketNumber, `{{department}}` → ticket's department name
- [ ] Unknown tokens (e.g., `{{custom_var}}`) are left as-is — no error
- [ ] Ticket not found or not accessible to caller returns `404`
- [ ] Template not found returns `404`

## Technical Notes
- Endpoint: `POST /agents/me/templates/{id}/render`
- Entity: `QuickReplyTemplate`, `Ticket`
- Business rules: BR-AGT-014, BR-AGT-015
- Spec: `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-060, US-BE-019
