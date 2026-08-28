# Feature Spec — Communication Channels

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


> Requirements: `REQ-COM-*`
> API: `specs/api/communication.md`
> Domain entities: `Ticket`, `TicketMessage`, `ChatSession`, `ChatSessionMessage`

---

## Overview

The CRM receives customer messages via five channels: Email, WhatsApp, SMS, Live Chat (SignalR), and Portal (web form). Each channel has a distinct inbound handler. Outbound replies are dispatched automatically per-channel by a Hangfire job when an agent posts a customer-facing ticket message. The system normalises all channels into the same `Ticket`/`TicketMessage` data model.

---

## Channel Capabilities Matrix

| Channel | Inbound | Outbound | Attachments | Real-time |
|---------|---------|---------|------------|----------|
| Email | Yes (SMTP relay) | Yes (SMTP) | Yes | No |
| WhatsApp | Yes (Twilio) | Yes (Twilio) | Yes (media) | No |
| SMS | Yes (Twilio) | Yes (Twilio) | No | No |
| Live Chat | Yes (SignalR) | Yes (SignalR) | No (v1) | Yes |
| Portal | Yes (REST POST) | Push via SignalR NotificationHub | Yes | Partial |

---

## Email Channel

### Inbound

**BR-COM-001** Inbound email arrives at a configured relay (e.g., SendGrid Inbound Parse, Postmark). The relay calls `POST /webhooks/email` with parsed fields.

**BR-COM-002** Signature validation: the relay HMAC-SHA256 signature is verified against the request payload using the configured webhook secret. Invalid signatures return `401`.

**BR-COM-003** Deduplication: before processing, check `TicketMessage.ExternalMessageId` for the incoming `Message-ID` header value. If a record already exists with that `ExternalMessageId`, the message is dropped silently (return `200`). This handles relay retries.

**BR-COM-004** Thread matching order:
  1. `In-Reply-To` or `References` header matches a known `TicketMessage.ExternalMessageId` → append to that ticket.
  2. `From` email matches a Customer with exactly one open Email ticket in any department → append to that ticket.
  3. `From` email matches a Customer with multiple open Email tickets → create a new ticket (ambiguous thread; flag for agent review).
  4. `From` email matches no Customer → create a new Customer record with `FullName` from the `From` display name, `Email` from the address, then create a new ticket.

**BR-COM-005** The email plain-text body is stored as `TicketMessage.Content`. HTML body is stored in `TicketMessage.ContentHtml` (nullable). The portal and agent UI display the plain-text version; raw HTML is available for display if plain-text is empty.

**BR-COM-006** Attachments in inbound email: downloaded from relay and uploaded to S3 via `StoreAttachmentJob` (Hangfire). If any attachment exceeds 5 MB, it is dropped and a system note is appended to the ticket: "Attachment `{filename}` was too large (>{size}MB) and was not saved."

**BR-COM-007** Inbound email response is always `200 OK`, even if processing fails internally. Errors are logged and sent to a `FailedInboundEmail` dead-letter queue for manual review.

### Outbound

**BR-COM-008** When an agent posts a message on an Email ticket with `IsInternal = false`, a Hangfire job `SendEmailJob` is enqueued immediately after the `TicketMessage` is persisted.

**BR-COM-009** Outbound email headers:
- `From`: `{Department Name} Support <noreply@{configured-domain}>`
- `Reply-To`: `{ticket-thread-address}@inbound.{configured-domain}` (routes replies back to the inbound handler with correct thread context)
- `Subject`: `Re: {ticket.Subject} [#{ticket.TicketNumber}]`
- `In-Reply-To` and `References`: set to the last `TicketMessage.ExternalMessageId` in the thread

**BR-COM-010** Retry policy: 3 attempts (T+1min, T+5min, T+15min). After 3 failures, `TicketMessage.DeliveryStatus = Failed` and agent receives a notification.

---

## WhatsApp Channel

### Inbound

**BR-COM-011** Twilio calls `POST /webhooks/whatsapp` with `X-Twilio-Signature` header. Validated against the Twilio auth token using `Twilio.Security.RequestValidator`.

**BR-COM-012** Phone number normalisation: strip the `whatsapp:` prefix and normalise to E.164 format (e.g., `+966501234567`) before customer lookup.

**BR-COM-013** Media files (`NumMedia > 0`): Twilio provides `MediaUrl{N}` URLs. The handler downloads each file from Twilio (authenticated), re-uploads to S3, and creates `TicketAttachment` records. `MediaContentType{N}` is used as the MIME type. Files > 5 MB are dropped with a system note (same as email BR-COM-006).

**BR-COM-014** Thread matching: same phone-to-Customer lookup as email (by `Customer.Phone`). If no Customer found, create one with `FullName = ProfileName` (WhatsApp display name) or `FullName = "Unknown ({phone})"` if no profile name.

**BR-COM-015** Response to Twilio must be a TwiML `<Response/>` body. An empty response (no reply) is used — actual reply is sent via the outbound job, not the webhook response.

### Outbound

**BR-COM-016** Outbound WhatsApp uses the Twilio WhatsApp API (`POST /2010-04-01/Accounts/{accountSid}/Messages.json`). Parameters: `From = whatsapp:{ourNumber}`, `To = whatsapp:{customerPhone}`, `Body = {messageContent}`.

**BR-COM-017** WhatsApp Business Policy constraint: after 24 hours of no customer-initiated message, outbound messages must use an approved WhatsApp template (HSM). In v1, the system warns the agent if the last customer message was > 23 hours ago: "Note: WhatsApp 24-hour window expires soon. Reply promptly or use a template." Template management is out of v1 scope.

---

## SMS Channel

### Inbound

**BR-COM-018** Same Twilio signature validation as WhatsApp. SMS has no `whatsapp:` prefix.

**BR-COM-019** SMS thread matching: by `Customer.Phone`. If multiple SMS segments arrive for the same Twilio `MessageSid` (Twilio concatenates long messages), the handler deduplicates by `ExternalMessageId = MessageSid`.

**BR-COM-020** No attachments for SMS.

### Outbound

**BR-COM-021** Outbound SMS uses Twilio SMS API. `Body` must be ≤ 1600 characters. If `TicketMessage.Content` exceeds 1600 chars, it is truncated to 1597 chars + "..." and the agent is warned: "SMS truncated to 1600 characters."

**BR-COM-022** SMS outbound is plain text only — Markdown is stripped before sending.

---

## Live Chat Channel

### Session Lifecycle

**BR-COM-023** A live chat session begins when a customer uses the portal chatbot and a handoff is required. The ChatHub `RequestHandoff` call creates a `Ticket` with `Channel = LiveChat` from the `ChatSession` transcript.

**BR-COM-024** An agent accepts the handoff via `AgentAcceptHandoff` on ChatHub. `ChatSession.Status = AgentConnected`, `ChatSession.AgentId = agentId`.

**BR-COM-025** All messages after agent connection are stored as `ChatSessionMessage` records AND as `TicketMessage` records on the created ticket (dual-write). This ensures the ticket has a complete thread even after the chat session ends.

**BR-COM-026** Session closure (`CloseSession`): `ChatSession.Status = Closed`. If `resolution = Resolved`, the linked Ticket is also moved to `Resolved`. If `resolution = Escalated`, a standard ticket escalation is triggered.

**BR-COM-027** Agent typing indicators (`AgentTyping`): broadcast to the customer's connection in the session group. Not persisted. Fire-and-forget.

**BR-COM-028** `context = "agent"` chat sessions (internal assistant, not customer-facing) do NOT create tickets and do not trigger handoff logic. They are purely conversational AI sessions.

---

## Portal Channel

**BR-COM-029** Portal is not a real-time bidirectional channel. Ticket creation via `POST /portal/tickets` is treated as a standard ticket with `Channel = Portal`. Messages are added via `POST /portal/tickets/{id}/messages`.

**BR-COM-030** Outbound from agent to portal customer: no email or SMS is sent. The customer is notified via SignalR `NotificationHub` (`TicketReplyReceived` notification). The customer sees the new message when they refresh the portal.

---

## Channel Admin Configuration

**BR-COM-031** Channel configuration (SMTP credentials, Twilio SID/token, webhook URLs) is stored in environment variables / Azure Key Vault. Not in the database. Not exposed via admin API.

**BR-COM-032** `GET /admin/channels/status` reads configuration existence and connectivity:
- Email: checks SMTP connection handshake
- WhatsApp/SMS: checks Twilio account status API
- Live Chat: reads count of active SignalR connections from the ChatHub context

---

## Outbound Delivery Status

`TicketMessage.DeliveryStatus` values:

| Status | Meaning |
|--------|---------|
| `NotApplicable` | Internal note or portal/chat message (no external delivery) |
| `Pending` | Outbound job enqueued, not yet sent |
| `Sent` | Provider accepted the message |
| `Failed` | All retries exhausted, delivery failed |

**BR-COM-033** `DeliveryStatus` is updated by the Hangfire job after each attempt. Agents see the delivery status badge on each message in the ticket thread.

---

## Acceptance Criteria

**AC-COM-001** Given a Twilio webhook call with an invalid `X-Twilio-Signature`, then the response is `403 Forbidden` and no ticket or message is created.

**AC-COM-002** Given an inbound email with a `Message-ID` that already exists in `TicketMessage.ExternalMessageId`, then the email is silently dropped and the response is `200 OK` with no duplicate message created.

**AC-COM-003** Given an inbound WhatsApp message with a media attachment of 6 MB, then the attachment is not stored, a system note "Attachment was too large" is appended to the ticket, and the response is `200 OK`.

**AC-COM-004** Given a ticket reply on an Email ticket, when the outbound Hangfire job runs, then the sent email has a `Reply-To` header set to the ticket thread address.

**AC-COM-005** Given an outbound SMS with content of 1650 characters, when the SMS is sent via Twilio, then the sent text is truncated to 1597 characters + "...".

**AC-COM-006** Given a customer sends an inbound email with no existing Customer record for that email, then a new Customer record is created and a new Ticket is created with `Channel = Email`.

**AC-COM-007** Given a live chat session is closed with `resolution = Resolved`, then the linked Ticket's Status becomes Resolved.

**AC-COM-008** Given `GET /admin/channels/status`, then all five channels are represented in the response, regardless of whether they are currently active.

---

## Edge Cases

- **Email loop detection**: if the `From` address matches our own `noreply@` domain, the email is dropped immediately (prevents auto-reply loops).
- **WhatsApp business verification**: if the Twilio account is not WhatsApp-approved, inbound webhooks still arrive but outbound fails. The error is surfaced as `DeliveryStatus = Failed` with reason "WhatsApp not approved."
- **SMS from a toll-free number**: Twilio may format `From` differently for toll-free. Normalise all inputs to E.164 before lookup.
- **Concurrent inbound messages from same customer**: if two messages arrive within 1 second from the same customer with the same phone number and both would create new tickets (first message not yet committed), use a database unique constraint on `(CustomerId, Status=New, Channel)` to prevent duplicate ticket creation — second insert fails, triggering append logic.

---

## Integration Points

| Event | Downstream |
|-------|-----------|
| Inbound message received (any channel) | Ticket module (create or append), SLA (start/update clock), Notifications (agent) |
| Agent posts outbound message | Communication (dispatch SendJob per channel) |
| WhatsApp 24h window warning | Agent notification |
| Chat handoff | ChatHub (real-time), Ticket (create from session) |
| Delivery failure | Notifications (agent), `TicketMessage.DeliveryStatus = Failed` |
