# US-FE-015 — Attachment Upload & List

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
**Roles:** Admin, Manager, Agent, Customer
**As an** agent, **I want to** upload and view attachments on a ticket, **so that** screenshots and documents are part of the ticket record.

## Acceptance Criteria
- [ ] Attachment list in ticket detail "Attachments" tab: filename, size, uploaded by, date, download link
- [ ] Upload zone: drag-and-drop or click-to-browse; validates file size < 5MB client-side before upload
- [ ] Upload progress bar per file
- [ ] On `422 ATTACHMENT_LIMIT_EXCEEDED` or quota errors: shows specific error message
- [ ] Delete button (Agent+ only): confirmation dialog; soft-deletes on confirm
- [ ] Customer view (portal): shows attachments but no delete button

## Technical Notes
- Component: `AttachmentPanelComponent`
- Services: `TicketService.uploadAttachment()`, `TicketService.deleteAttachment()`
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-030, US-BE-031, US-FE-010
