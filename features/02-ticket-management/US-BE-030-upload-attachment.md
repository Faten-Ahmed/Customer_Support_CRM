# US-BE-030 — Upload Ticket Attachment

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
**As an** agent or customer, **I want to** attach files to a ticket, **so that** screenshots or documents are part of the ticket record.

## Acceptance Criteria
- [ ] `POST /tickets/{id}/attachments` accepts `multipart/form-data` with one file per request
- [ ] Max file size: 5 MB; exceeding returns `422` with code `ATTACHMENT_LIMIT_EXCEEDED`
- [ ] Max total per ticket: 10 MB; exceeding returns `422` with code `TICKET_ATTACHMENT_QUOTA_EXCEEDED`
- [ ] Max total per customer across all tickets: 50 MB; exceeding returns `422` with code `CUSTOMER_ATTACHMENT_QUOTA_EXCEEDED`
- [ ] File stored in S3 (MinIO in dev) under `attachments/{ticketId}/{uuid}-{filename}`
- [ ] `TicketAttachment` record created with `StorageKey`, `FileName`, `FileSize`, `MimeType`
- [ ] Returns `201` with attachment metadata

## Technical Notes
- Endpoint: `POST /tickets/{id}/attachments`
- Entity: `TicketAttachment`
- Business rule: BR-TKT-007
- Spec: `specs/api/tickets.md`

## Dependencies
- US-BE-019, US-BE-007
