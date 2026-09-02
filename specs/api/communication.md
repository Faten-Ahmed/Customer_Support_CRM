# API Spec — Communication Channels

> Base path: `/webhooks` (inbound) | No REST base for outbound (triggered internally on ticket message send)
> Inbound webhooks are public endpoints — no Bearer auth. Each provider uses its own signature verification.
> Outbound delivery is handled internally by Hangfire jobs after a TicketMessage is created.

---

## Inbound Channel Architecture

When a message arrives via any external channel:

1. The webhook handler verifies the provider signature.
2. The system looks up the sender (email / phone number) against existing Customers.
3. If the sender matches an open ticket thread → message is appended as a new `TicketMessage`.
4. If no open thread exists → a new Ticket is created (`Channel` = the source channel, `Status = New`).
5. A `TicketMessageReceived` domain event fires → SignalR pushes update to assigned agent.

---

## POST /webhooks/email

Inbound email from the configured SMTP relay (e.g., SendGrid Inbound Parse, Gmail forwarding, or Postmark).

**Auth:** HMAC-SHA256 signature header (provider-specific). 401 if invalid.

**Content-Type:** `multipart/form-data` (SendGrid parse format) or provider equivalent.

**Fields the handler extracts:**

| Field | Source |
|-------|--------|
| `from` | `From` email address |
| `subject` | `Subject` header |
| `text` | Plain-text body |
| `html` | HTML body (stored, plain-text used for display) |
| `attachments` | Attached files (stored to S3, linked to ticket) |
| `inReplyTo` | `In-Reply-To` / `References` headers (thread matching) |
| `messageId` | `Message-ID` header (deduplication) |

**Thread matching logic:**

1. If `In-Reply-To` or `References` contains a known `ExternalMessageId` → append to that ticket's thread.
2. Else if `From` email matches a Customer with an open Email ticket → append to the newest open ticket.
3. Else → create a new Ticket (`Channel = Email`).

**Response 200:**
```json
{ "ok": true }
```

Return `200` even on processing errors — SMTP relays retry on non-2xx which causes duplicates. Log errors internally.

**Errors logged (not returned):** unknown sender, attachment too large, duplicate `messageId`

---

## POST /webhooks/whatsapp

Inbound WhatsApp message from Twilio.

**Auth:** Twilio signature header `X-Twilio-Signature` validated with account auth token. Returns `403` if invalid.

**Content-Type:** `application/x-www-form-urlencoded` (Twilio standard)

**Key fields:**

| Field | Description |
|-------|-------------|
| `From` | `whatsapp:+966501234567` |
| `To` | Our Twilio WhatsApp number |
| `Body` | Message text |
| `NumMedia` | Count of attached media |
| `MediaUrl{N}` | URL of Nth media file (downloaded and stored to S3) |
| `MessageSid` | Twilio message ID (deduplication) |
| `ProfileName` | WhatsApp display name (used if Customer not found) |

**Thread matching:**

1. Strip `whatsapp:` prefix, match phone number against Customer records.
2. If Customer has an open WhatsApp ticket → append message.
3. Else → create new Ticket (`Channel = WhatsApp`, auto-populate Subject from first message text, truncated to 100 chars).

**Response 200:**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Response></Response>
```

Twilio expects TwiML response. Empty `<Response/>` = no auto-reply (system sends reply via agent action).

---

## POST /webhooks/sms

Inbound SMS from Twilio.

**Auth:** Same Twilio signature validation as WhatsApp (`X-Twilio-Signature`). Returns `403` if invalid.

**Content-Type:** `application/x-www-form-urlencoded`

**Key fields:**

| Field | Description |
|-------|-------------|
| `From` | `+966501234567` |
| `To` | Our Twilio SMS number |
| `Body` | SMS text (max 1600 chars, split across parts by Twilio) |
| `MessageSid` | Deduplication |
| `NumSegments` | SMS segment count |

**Thread matching:** Same logic as WhatsApp — match phone to Customer → open SMS ticket → else create new.

**Response 200:**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Response></Response>
```

---

## Outbound Delivery (Internal — Not a REST Endpoint)

When an agent posts a reply via `POST /tickets/{id}/messages`, the system:

1. Persists the `TicketMessage` record.
2. Enqueues a Hangfire job `SendOutboundMessageJob` based on `Ticket.Channel`:
   - **Email** → SMTP send via configured relay (From: department noreply address, Reply-To: ticket thread address, `In-Reply-To` and `References` set for thread continuity)
   - **WhatsApp** → Twilio WhatsApp API `POST /2010-04-01/Accounts/{sid}/Messages`
   - **SMS** → Twilio SMS API `POST /2010-04-01/Accounts/{sid}/Messages`
   - **Portal** → no outbound send (message visible in portal instantly via SignalR)
   - **LiveChat** → no outbound send (message delivered via ChatHub in real time)

Delivery status (`Sent`, `Failed`, `Pending`) is stored on `TicketMessage.DeliveryStatus`. Agents see delivery status in the ticket thread UI.

**Retry policy:** 3 attempts with exponential back-off (1 min, 5 min, 15 min). After 3 failures, `DeliveryStatus = Failed` and agent is notified.

---

## SignalR — ChatHub

Real-time live chat between customer and agent. Used for both the AI chatbot handoff and direct live chat sessions.

**Hub URL:** `ws://localhost:5000/hubs/chat?access_token=<token>`

**Roles:** `[Any]` — customers connect with Customer JWT; agents with Agent+ JWT.

---

### Connection & Session Management

A `ChatSession` is created server-side when a customer starts a chat (`POST /ai/chat/message` with `sessionId: null`). The returned `sessionId` is used to join the SignalR group.

**Client → Server:**

#### `JoinSession`
Join a chat session room.

```json
{ "sessionId": "uuid" }
```

Server validates the caller owns or is assigned to the session. Adds connection to group `chat-{sessionId}`.

#### `LeaveSession`
```json
{ "sessionId": "uuid" }
```

---

### Handoff Flow

When `POST /ai/chat/message` returns `handoffRequired: true`, the portal client:

1. Calls `JoinSession` on ChatHub.
2. Emits `RequestHandoff` to notify available agents.

**Client → Server:**

#### `RequestHandoff`
```json
{
  "sessionId": "uuid",
  "reason": "Customer requested human agent"
}
```

Server:
- Creates a Ticket from the ChatSession transcript (`Channel = LiveChat`).
- Broadcasts `HandoffRequested` to agents in the relevant department group.

#### `SendMessage`
Customer or agent sends a message in an active live chat.

```json
{
  "sessionId": "uuid",
  "content": "I still can't log in after resetting."
}
```

Server persists message to `ChatSessionMessage`, broadcasts to session group.

#### `AgentAcceptHandoff`
Agent accepts a pending handoff.

```json
{ "sessionId": "uuid" }
```

Server assigns agent to `ChatSession.AgentId`, updates `ChatSession.Status = AgentConnected`.

#### `CloseSession`
Agent or customer closes the live chat.

```json
{
  "sessionId": "uuid",
  "resolution": "Resolved"
}
```

Allowed `resolution` values: `Resolved`, `Escalated`, `Abandoned`.

---

### Server → Client Methods

| Method | Payload | Sent to |
|--------|---------|---------|
| `ReceiveMessage` | `{ sessionId, senderName, senderRole, content, sentAt }` | Session group |
| `HandoffRequested` | `{ sessionId, customerName, preview, requestedAt }` | Department agent group |
| `HandoffAccepted` | `{ sessionId, agentName, agentId }` | Session group |
| `SessionClosed` | `{ sessionId, resolution, closedAt }` | Session group |
| `AgentTyping` | `{ sessionId }` | Customer in session |
| `CustomerTyping` | `{ sessionId }` | Agent in session |

---

### Agent Group Subscription

Agents subscribe to their department's live chat queue on connection:

**Client → Server:**

#### `SubscribeToDepartment`
```json
{ "departmentId": "uuid" }
```

Server validates agent belongs to that department. Adds connection to group `dept-chat-{departmentId}`.

---

## Channel Status Endpoint

### GET /admin/channels/status

Check connectivity of all configured inbound/outbound channel integrations.

**Auth:** Bearer | **Roles:** `[Admin]`

**Response 200:**
```json
{
  "data": {
    "email": {
      "inbound": { "configured": true, "lastMessageAt": "2025-10-15T09:00:00Z" },
      "outbound": { "configured": true, "provider": "SendGrid" }
    },
    "whatsapp": {
      "inbound": { "configured": true, "lastMessageAt": "2025-10-15T10:30:00Z" },
      "outbound": { "configured": true, "provider": "Twilio", "number": "+966920000000" }
    },
    "sms": {
      "inbound": { "configured": true, "lastMessageAt": "2025-10-14T16:00:00Z" },
      "outbound": { "configured": true, "provider": "Twilio", "number": "+966910000000" }
    },
    "liveChat": {
      "configured": true,
      "activeSessions": 3,
      "pendingHandoffs": 1
    },
    "portal": {
      "configured": true
    }
  }
}
```
