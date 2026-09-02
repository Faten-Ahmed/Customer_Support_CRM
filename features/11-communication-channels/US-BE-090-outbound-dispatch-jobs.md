# US-BE-090 — Outbound Message Dispatch Jobs

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
**Roles:** System (Hangfire jobs)
**As the** system, **I want to** deliver agent replies through the customer's original channel, **so that** customers receive responses where they originally contacted us.

## Acceptance Criteria
- [ ] On `TicketMessageAdded` (IsInternal=false, sender=Agent): enqueue `SendOutboundMessageJob` immediately
- [ ] Email job: sends via SMTP with `From`, `Reply-To`, `In-Reply-To`, `References` headers set correctly
- [ ] WhatsApp job: calls Twilio WhatsApp API; warns if last customer message > 23h (24h window)
- [ ] SMS job: truncates content to 1597 chars + "..." if > 1600 chars; strips Markdown
- [ ] Retry: 3 attempts (T+1min, T+5min, T+15min); after 3 failures → `DeliveryStatus = Failed`, agent notified
- [ ] Portal and LiveChat tickets: no outbound job (notification-only)

## Technical Notes
- Implementation: Hangfire `SendEmailJob`, `SendWhatsAppJob`, `SendSmsJob`
- Entity: `TicketMessage.DeliveryStatus`
- Business rules: BR-COM-008—010, BR-COM-016—022
- Spec: `specs/api/communication.md`

## Dependencies
- US-BE-028, US-BE-088, US-BE-089
