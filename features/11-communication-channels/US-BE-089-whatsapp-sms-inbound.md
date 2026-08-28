# US-BE-089 — WhatsApp & SMS Inbound Webhooks

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
**As the** system, **I want to** receive WhatsApp and SMS messages from Twilio, **so that** customers can contact support via mobile messaging.

## Acceptance Criteria
- [ ] `POST /webhooks/whatsapp`: validates `X-Twilio-Signature`; `403` if invalid
- [ ] Strips `whatsapp:` prefix; normalises phone to E.164 before customer lookup
- [ ] Media files: downloaded from Twilio, re-uploaded to S3; > 5MB dropped with system note
- [ ] `POST /webhooks/sms`: same Twilio signature validation
- [ ] SMS deduplication by `MessageSid` (= `ExternalMessageId`)
- [ ] Both return TwiML `<Response/>` (empty — no auto-reply)
- [ ] Unknown sender creates new `Customer` with phone; name = WhatsApp `ProfileName` or `Unknown ({phone})`

## Technical Notes
- Endpoints: `POST /webhooks/whatsapp`, `POST /webhooks/sms`
- Entity: `Ticket`, `TicketMessage`, `Customer`
- Business rules: BR-COM-011—020
- Spec: `specs/api/communication.md`

## Dependencies
- US-BE-088, US-BE-009
