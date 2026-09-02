# US-FE-014 — Reply Composer with Template Picker

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
**As an** agent, **I want to** compose replies with quick-reply templates and send them, **so that** I respond consistently and fast.

## Acceptance Criteria
- [ ] Textarea for composing reply (min height 80px, auto-grow)
- [ ] "Internal Note" toggle: switches background to yellow; label shown
- [ ] "Use Template" button: opens searchable template picker modal (personal + global templates listed)
- [ ] On template select: rendered content inserted into textarea (calls render API with current ticketId)
- [ ] Character counter visible
- [ ] Send button: disabled if textarea empty; shows spinner on submit
- [ ] After send: textarea cleared; message appears in thread immediately (optimistic update)

## Technical Notes
- Component: `ReplyComposerComponent`
- Services: `TicketService.addMessage()`, `TemplateService.render()`
- Spec: `specs/api/tickets.md`, `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-028, US-BE-061, US-FE-013
