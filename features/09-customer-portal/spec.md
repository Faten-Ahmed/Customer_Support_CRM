# Feature Spec — Customer Portal

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


> Requirements: `REQ-PLT-*`
> API: `specs/api/customer-portal.md`, `specs/api/auth.md` (portal-specific auth endpoints)
> Domain entities: `Customer`, `Ticket`, `TicketMessage`, `KbArticle`, `CsatSurvey`

---

## Overview

The Customer Portal is the self-service web interface for Customers. It allows ticket submission and tracking, knowledge base access, CSAT survey submission, and profile management. Customers see only their own data — no cross-customer visibility exists at any endpoint.

---

## Authentication & Access

**BR-PLT-001** The Customer Portal uses the same JWT mechanism as the internal app, but Customer JWTs are issued with `role = Customer` and have access only to `/portal/*` endpoints and `/auth/*` endpoints.

**BR-PLT-002** Customers who were created internally by an Agent (not self-registered) do not have a portal password. If they attempt to use "forgot password", they receive an email to set an initial password, effectively activating portal access.

**BR-PLT-003** Customer sessions are governed by the same token TTL (15-min access token, 7-day refresh HttpOnly cookie) as internal users.

**BR-PLT-004** A Customer accessing another customer's resource (ticket, survey, etc.) via a guessed ID receives `403 Forbidden`, not `404` — to avoid ID enumeration but still indicate unauthorized access.

---

## Profile Management

**BR-PLT-005** Customers can update: `fullName`, `phone`, `city`. They cannot update `email` (identity key), `companyName` (managed internally), or `country` via the portal.

**BR-PLT-006** `PUT /portal/profile` does not require all fields — partial update is supported (omitted fields are unchanged).

---

## Ticket Management (Portal)

### Creating Tickets

**BR-PLT-007** Portal ticket creation requires `departmentId`, `subject`, `description`. `categoryId` is optional (customer may not know their category).

**BR-PLT-008** Priority is always set to `Medium` on portal-created tickets. Customers cannot specify priority.

**BR-PLT-009** `Channel` is always set to `Portal` for portal-created tickets.

**BR-PLT-010** Required custom fields (`TicketFieldDefinition.IsRequired = true`) scoped to the selected Department must be provided in `customFieldValues`. The portal ticket form should dynamically fetch and render these fields from `GET /admin/field-definitions?departmentId=X` (public endpoint for the portal, filtered to active fields only).

### Viewing Tickets

**BR-PLT-011** `GET /portal/tickets` returns only tickets where `Ticket.CustomerId = caller.CustomerId`. No exceptions.

**BR-PLT-012** The ticket list does NOT include `description` (to keep payload small). Full details including description are returned by `GET /portal/tickets/{id}`.

**BR-PLT-013** Internal information is hidden from the portal: no `AssignedAgentId` UUID (only `assignedAgent.fullName` — first name only in v1 to protect agent privacy), no internal notes, no SLA breach percentages (only `resolutionDeadline` and `breachLevel` if applicable).

**BR-PLT-014** `GET /portal/tickets/{id}` returns `messagesCount` but not the messages themselves (fetched separately via pagination for performance).

### Replying to Tickets

**BR-PLT-015** `POST /portal/tickets/{id}/messages` creates a customer message with `IsInternal = false` and `SenderType = Customer`.

**BR-PLT-016** Replying to a `Resolved` ticket automatically reopens it (see `specs/features/02-ticket-management.md` W-TKT-04). The response body is the created message (not the reopened ticket). The client should re-fetch the ticket to reflect the status change.

**BR-PLT-017** Replying to a `Closed` ticket returns `422` with code `TICKET_CLOSED`. The customer must use "Reopen" first (separate action) — but in v1, reopening a closed ticket is only possible via the portal's explicit "Reopen" action, not by just replying.

### Closing Tickets

**BR-PLT-018** `POST /portal/tickets/{id}/close` is available from any open status (New, Assigned, InProgress, OnHold, Resolved). Moving from Resolved → Closed triggers the CSAT survey dispatch. Moving from other statuses → Closed also triggers the survey.

**BR-PLT-019** Closing an already-closed ticket returns `422` with code `TICKET_ALREADY_CLOSED`.

---

## Knowledge Base (Portal)

**BR-PLT-020** Portal KB endpoints return only `Published` articles with `Visibility IN ('Public', 'Both')`.

**BR-PLT-021** `GET /portal/kb/articles` supports `?search=` for inline search (full-text). If both `categoryId` and `search` are provided, both filters are applied (AND logic).

**BR-PLT-022** Article content is returned in full in `GET /portal/kb/articles/{id}` — both `content` (English Markdown) and `contentAr` (Arabic Markdown, may be null) are included. The portal client selects language based on the customer's profile preference or browser language.

**BR-PLT-023** `GET /portal/kb/search?q=` is a dedicated search endpoint (same results as using `?search=` on the articles endpoint, provided as a semantic URL for search UX).

---

## CSAT Survey (Portal)

**BR-PLT-024** Customers access pending surveys via `GET /portal/surveys/{id}`. The `id` is sent to the customer in their notification (`SurveyAvailable` type). A customer can also see their pending surveys via a "Feedback" section in the portal (listing API to be added in implementation if needed).

**BR-PLT-025** A survey expires after 7 days from `SentAt`. After expiry, `isExpired = true` in the GET response, and `POST /portal/surveys/{id}/submit` returns `422` with code `SURVEY_EXPIRED`.

**BR-PLT-026** A survey that has already been submitted returns `422` with code `SURVEY_ALREADY_SUBMITTED` on re-submit attempts.

**BR-PLT-027** `rating` must be an integer between 1 and 5 inclusive. Values outside this range return `422` with code `INVALID_RATING`.

**BR-PLT-028** `comment` is optional. If provided, max 1000 characters.

---

## Chatbot Integration (Portal)

The portal includes an AI chatbot widget powered by `POST /ai/chat/message`. From the portal customer's perspective:

**BR-PLT-029** The chatbot widget initiates with `context = "portal"` and `sessionId = null` to start a new session. The server returns a `sessionId` which is used for all subsequent messages in the conversation.

**BR-PLT-030** When the chatbot returns `handoffRequired = true`, the portal client:
1. Shows "Connecting you to an agent..." message.
2. Connects to `ChatHub` and calls `JoinSession + RequestHandoff`.
3. Transitions the chat widget into live-chat mode.

**BR-PLT-031** The customer's portal chatbot session (`context = "portal"`) is isolated from the agent-facing assistant (`context = "agent"`). Session histories are not shared.

---

## Acceptance Criteria

**AC-PLT-001** Given Customer A has 3 tickets, when Customer B (different account) calls `GET /portal/tickets/{ticketId of Customer A}`, then the response is `403 Forbidden`.

**AC-PLT-002** Given a portal ticket creation request with a required custom field (`Serial Number`) omitted, then the response is `422` listing the missing field.

**AC-PLT-003** Given a Customer replies to a ticket in Resolved status, when `POST /portal/tickets/{id}/messages` is processed, then the created message returns `201` and the ticket status becomes InProgress (verifiable via `GET /portal/tickets/{id}`).

**AC-PLT-004** Given a survey was sent 8 days ago, when the Customer submits the survey, then the response is `422` with code `SURVEY_EXPIRED`.

**AC-PLT-005** Given `POST /portal/tickets/{id}/close` when the ticket is in InProgress status, then the ticket status becomes Closed AND a CSAT survey is dispatched to the customer.

**AC-PLT-006** Given `GET /portal/tickets/{id}`, then the response does NOT include the agent's UUID or any internal notes (IsInternal=true messages).

**AC-PLT-007** Given a Customer calls `PUT /portal/profile` with `{"email": "new@example.com"}`, then the email field is ignored and the response shows the original email unchanged.

**AC-PLT-008** Given `GET /portal/kb/articles/{id}` for an article with `Visibility = Internal`, then the response is `403 Forbidden`.

---

## Edge Cases

- **Ticket with no messages**: `GET /portal/tickets/{id}` shows `messagesCount = 0`. Calling `GET /portal/tickets/{id}/messages` returns empty `data: []` with `totalCount = 0`.
- **Department with no custom fields**: `customFieldValues` in the request is accepted as empty `{}` or omitted.
- **Customer with no portal password (internally created)**: attempting to log in with any password returns `401`. "Forgot password" flow works normally and sets initial password.
- **Survey for a ticket the customer didn't close**: the system can send a CSAT survey even if the ticket was closed by an agent (not the customer). The customer still receives and can submit the survey.
- **Arabic UI**: the portal supports RTL layout. All `titleAr`, `contentAr`, and `nameAr` fields are provided for Arabic-speaking customers. The portal client handles language selection.

---

## Integration Points

| Portal Action | Downstream Effect |
|--------------|-------------------|
| Customer registers | Auth (email verification flow) |
| Customer submits ticket | Ticket module (create), SLA (start clock), Notifications (agent) |
| Customer replies | Ticket module (append message), Notifications (agent) |
| Customer closes ticket | CSAT (trigger survey), Notifications (agent) |
| Customer reopens (by replying to Resolved) | Ticket module (reopen flow), Notifications (agent) |
| CSAT survey submitted | Reports (CSAT data), Dashboard (KPI update) |
| Chatbot handoff | ChatHub (live chat session), Ticket (create from chat session) |
