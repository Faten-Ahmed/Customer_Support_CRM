# US-BE-072 — Channel Status Endpoint

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


**Epic:** Admin Configuration
**Roles:** Admin
**As an** admin, **I want to** see the connectivity status of all inbound/outbound channel integrations, **so that** I know if any channel is misconfigured or down.

## Acceptance Criteria
- [ ] `GET /admin/channels/status` returns status for all 5 channels: email, whatsapp, sms, liveChat, portal
- [ ] Email: checks SMTP connection handshake; reports `configured`, `lastMessageAt`
- [ ] WhatsApp/SMS: calls Twilio account status API to confirm credentials are valid
- [ ] LiveChat: reports `activeSessions` count and `pendingHandoffs`
- [ ] Portal: always `configured: true` (internal)
- [ ] Response always includes all 5 channels, even if unconfigured

## Technical Notes
- Endpoint: `GET /admin/channels/status`
- Business rule: BR-COM-032
- Spec: `specs/api/communication.md`

## Dependencies
- US-BE-007
