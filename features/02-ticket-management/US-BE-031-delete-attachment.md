# US-BE-031 — Delete Ticket Attachment

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
**As an** agent, **I want to** remove an attachment from a ticket, **so that** incorrect or sensitive files are not retained.

## Acceptance Criteria
- [ ] `DELETE /tickets/{id}/attachments/{attachmentId}` soft-deletes the record (`DeletedAt = now`); returns `204`
- [ ] Customer role returns `403` (customers cannot delete attachments)
- [ ] S3 object is NOT immediately deleted — nightly Hangfire job `PurgeOrphanedAttachmentsJob` removes orphaned objects
- [ ] Attachment belonging to a different ticket returns `404`

## Technical Notes
- Endpoint: `DELETE /tickets/{id}/attachments/{attachmentId}`
- Entity: `TicketAttachment`
- Business rule: BR-TKT-008
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-030
