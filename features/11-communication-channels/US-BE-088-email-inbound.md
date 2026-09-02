# US-BE-088 — Email Inbound Webhook

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


**Epic:** Communication Channels
**Roles:** System (webhook)
**As the** system, **I want to** receive and process inbound emails, **so that** customers can submit tickets by emailing our support address.

## Acceptance Criteria
- [ ] `POST /webhooks/email` validates HMAC-SHA256 signature; invalid returns `401`
- [ ] Deduplication: if `Message-ID` matches existing `TicketMessage.ExternalMessageId` → drop silently, return `200`
- [ ] Thread matching (in order): In-Reply-To/References match → append; phone/email matches open ticket → append; else create new ticket
- [ ] Unknown sender: auto-create `Customer` record from `From` display name + email
- [ ] Attachments > 5MB: dropped with system note appended to ticket
- [ ] Email loop detection: `From` matching our own noreply domain → drop immediately
- [ ] Always returns `200` even on processing errors (errors logged to dead-letter queue)

## Technical Notes
- Endpoint: `POST /webhooks/email`
- Entity: `Ticket`, `TicketMessage`, `TicketAttachment`, `Customer`
- Business rules: BR-COM-001—007
- Spec: `specs/api/communication.md`

## Dependencies
- US-BE-019, US-BE-009
