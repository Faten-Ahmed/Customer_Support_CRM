# Feature Spec — Ticket Management

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


> Requirements: `REQ-TKT-*`
> API: `specs/api/tickets.md`
> Domain entities: `Ticket`, `TicketMessage`, `TicketAttachment`, `TicketHistory`, `TicketFieldDefinition`, `TicketFieldValue`

---

## Overview

Tickets are the central aggregate of the CRM. A ticket represents a single support request from a Customer, received via any channel, tracked through a defined status lifecycle, and resolved by one or more Agents. Every action on a ticket is logged to an immutable history.

---

## Ticket Status State Machine

```
                ┌──────────────────────────────────────────┐
                ▼                                          │
  [New] ──assign──▶ [Assigned] ──start──▶ [InProgress]   │ (Reopen)
                        │                      │           │
                        │                  ┌───┴───┐       │
                        │                  ▼       ▼       │
                        │              [OnHold] [Escalated]│
                        │                  │       │       │
                        │                  └───┬───┘       │
                        │                      ▼           │
                        └──────────────▶ [Resolved] ──────▶│
                                              │
                                              ▼
                                          [Closed]
```

### Allowed Transitions

| From | To | Who Can Trigger |
|------|----|----------------|
| New | Assigned | System (auto-assign), Agent (self-assign), Admin/Manager (manual assign) |
| Assigned | InProgress | Assigned Agent |
| InProgress | OnHold | Assigned Agent, Manager |
| InProgress | Escalated | Agent (escalation request), System (SLA CriticalBreach) |
| InProgress | Resolved | Assigned Agent |
| OnHold | InProgress | Assigned Agent, Manager |
| Escalated | InProgress | Manager (de-escalate + reassign), Admin |
| Resolved | Closed | System (auto-close after 48h with no reply), Customer (portal close), Agent |
| Resolved | Reopened | Customer (replies to resolved ticket), Agent |
| Reopened | Assigned | System (re-assigns to original agent if available) |
| Any open state | Closed | Customer (portal), Admin |

**Note:** "Reopened" is a transitional pseudo-status — the ticket immediately transitions to `Assigned` in the same operation. `Reopened` appears in history but is never a resting state.

---

## Business Rules

**BR-TKT-001** Every ticket must belong to exactly one Department. Department is set at creation and can only be changed via `POST /tickets/{id}/transfer`.

**BR-TKT-002** `TicketNumber` is auto-generated as `TKT-{YEAR}-{SEQUENCE}` where SEQUENCE is a zero-padded 5-digit integer, per-year, per-organization (e.g., `TKT-2025-00043`). The sequence resets each January 1.

**BR-TKT-003** Priority defaults to `Medium` if not provided by the creating system. Channel-specific overrides:
  - Email subject containing keywords `urgent`, `critical`, `asap` → `High`
  - Portal creation → always `Medium` (customer cannot set priority)
  - Admin/Agent creation → any priority allowed

**BR-TKT-004** A Customer can only have one `Active` ticket (Status not Closed) per Department at a time. If a second inbound message arrives that would create a new ticket for the same Department, it is appended to the existing open ticket as a new message. Exception: Admin/Agent can force-create a second ticket with `forceNew: true`.

**BR-TKT-005** Custom field values must conform to their `TicketFieldDefinition` for the ticket's Department. Required fields must be provided at creation. Unknown field IDs are rejected.

**BR-TKT-006** The `IsInternal` flag on `TicketMessage` determines visibility: `true` = internal note (Agents/Managers/Admins only); `false` = customer-facing (visible in portal and sent via outbound channel). A Customer cannot post an internal note.

**BR-TKT-007** Attachments are limited to 5 MB per file, 10 MB total per ticket, 50 MB total per customer across all their tickets. Exceeding any limit returns `422` with code `ATTACHMENT_LIMIT_EXCEEDED`.

**BR-TKT-008** Deleting an attachment (`DELETE /tickets/{id}/attachments/{attachmentId}`) soft-deletes the record (sets `DeletedAt`) but does not immediately remove the file from S3. A nightly Hangfire job purges orphaned S3 objects. Customers cannot delete attachments.

**BR-TKT-009** `POST /tickets/{id}/status` to `Resolved` must include a `resolution` string (min 10 chars). This is stored on the Ticket and displayed to the customer.

**BR-TKT-010** `POST /tickets/{id}/escalate` requires either `reason` (free text, min 20 chars) or a selection from predefined escalation reasons. The escalated ticket enters a manager-owned queue visible at `GET /tickets?status=Escalated`.

**BR-TKT-011** Transferring a ticket (`POST /tickets/{id}/transfer`) changes `DepartmentId` and clears `AssignedAgentId`. A `TransferNote` is required. The ticket status reverts to `New` in the new department. A `TicketTransferred` history entry is created.

**BR-TKT-012** Once a ticket is `Closed`, no new messages can be added (returns `422` with code `TICKET_CLOSED`). Customers can re-open a closed ticket by calling `POST /tickets/{id}/reopen`.

**BR-TKT-013** Ticket history entries (`TicketHistory`) are append-only and immutable. No update or delete endpoint exists. They are the single source of truth for the audit trail.

---

## Workflows

### W-TKT-01: Ticket Creation (Any Channel)
1. Source (webhook handler, portal controller, or API call) constructs `CreateTicketCommand`.
2. Check BR-TKT-004: does customer have an open ticket in this department? If yes and `forceNew != true`, append message to existing ticket → skip to step 6.
3. Validate custom fields against `TicketFieldDefinition` for the department.
4. Generate `TicketNumber` (sequence lock, increment, release).
5. Create `Ticket` record. Create initial `TicketMessage` with `IsInternal = false`.
6. Enqueue SLA clock start: `StartSlaClockJob` (calculates `FirstResponseDeadline`, `ResolutionDeadline` based on business hours + SLA policy for the ticket's priority + department).
7. Trigger auto-assignment: `AutoAssignTicketJob` (see Assignment workflow below).
8. Publish domain event `TicketCreated`.
9. Notify department agents via SignalR `ReceiveNotification`.

### W-TKT-02: Auto-Assignment
Executed by `AutoAssignTicketJob`:

1. **Skills-based match**: find Agents in the ticket's Department whose `AgentSkill.CategoryId` matches the ticket's `CategoryId`. Among those, find the one with the fewest open tickets (`Status` in `Assigned`, `InProgress`). Assign if found.
2. **Round-robin fallback**: if no skills match, find all active Agents in the Department sorted by `LastAssignedAt ASC`. Assign the next in rotation.
3. **Pull queue fallback**: if all Agents are at capacity (> 15 open tickets each), ticket stays `Status = New` in the unassigned queue. Agents can pull from `GET /tickets/unassigned`.
4. **Manager escalation**: if no Agents are active in the Department (all offline or unavailable), notify Department Manager via `ReceiveNotification` with `type = UnassignedTicketAlert`.

On assignment: `Ticket.AssignedAgentId = agentId`, `Ticket.Status = Assigned`, `Ticket.AssignedAt = now`. History entry created. SLA first-response clock continues.

### W-TKT-03: First Response SLA Clock
- Clock starts when the ticket is created.
- Clock pauses when `Status = OnHold` (waiting on customer or third party).
- Clock resumes when `Status` transitions back to `InProgress`.
- Clock stops (met) when first customer-facing message (`IsInternal = false`) is posted by an Agent.
- If `FirstResponseDeadline` is crossed and no agent reply exists → `SlaBreachLevel = Warning` at 80%, `Breach` at 100%, `CriticalBreach` at 200%.

### W-TKT-04: Customer Reply to Resolved Ticket (Reopen)
1. Customer posts message via `POST /portal/tickets/{id}/messages` when ticket is `Resolved`.
2. System detects status = Resolved + new customer message.
3. Ticket status → `InProgress` (logged as "Reopened" in history).
4. If original Agent is still active in the Department → reassign to them.
5. Else → re-enter auto-assignment flow (W-TKT-02).
6. Notify agent and manager via notification.
7. SLA clock resumes (resolution deadline recalculated).

### W-TKT-05: Auto-Close After 48 Hours
1. Hangfire scheduled job `AutoCloseResolvedTicketsJob` runs every 30 minutes.
2. Finds all tickets with `Status = Resolved` and `ResolvedAt < now - 48h` with no customer message since `ResolvedAt`.
3. For each: sets `Status = Closed`, `ClosedAt = now`, `ClosedBy = System`.
4. Triggers CSAT survey dispatch (see `specs/features/12-csat-surveys.md`).
5. Publishes `TicketClosed` domain event.

---

## Custom Fields

`TicketFieldDefinition` defines fields at Department level (optionally scoped to a Category too). Types:
- `Text` — free text NVARCHAR
- `Number` — stored as string, validated as numeric
- `Date` — ISO 8601 date string
- `Dropdown` — value must be one of `TicketFieldDefinition.Options[]`
- `Checkbox` — `"true"` or `"false"` string

`Ticket.CustomFieldValues` is a JSON column: `{ "field-def-uuid": "value", ... }`.

Validation at ticket creation and update: for each active `TicketFieldDefinition` in the ticket's Department (and Category if set):
- If `IsRequired = true` and no value provided → reject with `422` listing missing field names.
- If type is `Dropdown` and value not in `Options` → reject.

---

## Acceptance Criteria

**AC-TKT-001** Given a ticket with Status = Closed, when a customer posts a message via the portal, then the response is `422` with code `TICKET_CLOSED`.

**AC-TKT-002** Given a ticket is created at 09:00 with Critical priority (SLA: 15-min first response), when no agent reply exists at 09:16, then `SlaBreachLevel = Breach` and a `SlaBreached` notification is sent to the assigned agent and department manager.

**AC-TKT-003** Given a ticket transfer to Department B, when the transfer completes, then `AssignedAgentId` is null, `Status = New`, and the ticket appears in Department B's unassigned queue.

**AC-TKT-004** Given an Agent posts a message with `isInternal = true`, when a Customer views the ticket via the portal, then the internal note is not present in the response.

**AC-TKT-005** Given a file upload of 5.1 MB, when `POST /tickets/{id}/attachments` is called, then the response is `422` with code `ATTACHMENT_LIMIT_EXCEEDED`.

**AC-TKT-006** Given `TicketFieldDefinition` for Department A with field "Serial Number" (`isRequired = true`), when a ticket is created for Department A without the Serial Number value, then the response is `422` listing the missing required field.

**AC-TKT-007** Given a ticket has been Resolved for 49 hours with no customer reply, when the AutoCloseJob runs, then the ticket Status is Closed and a CSAT survey is dispatched.

**AC-TKT-008** Given ticket Status = Resolved, when a customer posts a reply via the portal, then the ticket Status becomes InProgress and a history entry with action "Reopened" is logged.

**AC-TKT-009** Given two agents in Department A both with >15 open tickets, when a new ticket arrives, then the ticket stays New and a manager notification of type `UnassignedTicketAlert` is sent.

---

## Edge Cases

- **Channel mismatch on reply**: if `Ticket.Channel = WhatsApp` and an email arrives from the same customer referencing the same thread, the email is appended as a new message on the existing ticket.
- **Agent self-assignment**: an Agent can `POST /tickets/{id}/assign` to assign themselves even if the auto-assign already picked another agent (last write wins; both entries logged to history).
- **Priority escalation**: after escalation, the Manager may increase ticket priority. SLA deadlines are recalculated based on the new priority at the time of change.
- **Deleted customer**: if a customer is soft-deleted, their existing tickets remain accessible internally. No new messages can be received from them.

---

## Integration Points

| Event Published | Consumed By |
|----------------|-------------|
| `TicketCreated` | SLA (start clock), Notifications, Reports |
| `TicketAssigned` | Notifications (agent), SLA, Reports |
| `TicketStatusChanged` | SLA (pause/resume/stop clock), Notifications, Dashboard |
| `TicketMessageAdded` | Notifications (agent/customer), Communication (outbound send) |
| `TicketEscalated` | Notifications (manager), Reports |
| `TicketClosed` | CSAT (trigger survey), Reports, Dashboard |
| `TicketTransferred` | Notifications (new dept manager/agents), SLA (recalculate) |
| `SlaBreached` | Notifications (agent, manager), Dashboard |
