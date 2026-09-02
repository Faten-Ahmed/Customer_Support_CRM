# Feature Spec — Agent Dashboard

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


> Requirements: `REQ-AGT-*`
> API: `specs/api/agent-dashboard.md`
> Domain entities: `User`, `Ticket`, `AgentTask`, `QuickReplyTemplate`

---

## Overview

The Agent Dashboard is the primary workspace for support agents. It provides a personalized view of assigned tickets, availability control, personal task tracking, and quick-reply templates. It is designed for speed — agents spend most of their working day in this view.

---

## My Tickets View

### Business Rules

**BR-AGT-001** `GET /agents/me/tickets` returns only tickets where `AssignedAgentId = caller.UserId`. No cross-agent visibility at this endpoint.

**BR-AGT-002** Default sort: `Priority DESC` (Critical first), then `SLA urgency ASC` (nearest deadline first). Agent may override sort via `?sortBy` and `?sortDir` params.

**BR-AGT-003** Each ticket item in the response includes a live SLA indicator:
- `slaStatus`: `ok` / `warning` / `breach` / `criticalBreach`
- `resolutionRemainingMinutes`: pre-computed at query time (not real-time — within 5 minutes of actual value due to the SLA monitor job cadence)

**BR-AGT-004** Filter by `status`, `priority`, `categoryId`, `dateFrom`, `dateTo` is supported. Agents can filter to `status=OnHold` to see tickets they've put on hold.

**BR-AGT-005** An Agent can see tickets assigned to them even if the ticket's Department is not their primary department (multi-department agents can work across departments).

### Unassigned Pull Queue

**BR-AGT-006** `GET /tickets/unassigned` shows tickets in `Status = New` with no `AssignedAgentId`, filtered to Departments the calling agent belongs to. Sorted by creation time ASC (oldest first).

**BR-AGT-007** An Agent can self-assign from the pull queue by calling `POST /tickets/{id}/assign` with their own `agentId`. This is a first-come-first-served operation — concurrent self-assigns are prevented by a row-level lock on the Ticket record.

---

## Availability Status

### Statuses

| Status | Meaning | Tickets Routed |
|--------|---------|---------------|
| `Available` | Ready to receive tickets | Yes |
| `Busy` | Actively working, do not auto-assign new | No |
| `Away` | Temporarily away | No |
| `Offline` | Not logged in / end of shift | No |

**BR-AGT-008** Only `Available` agents receive new auto-assigned tickets. The auto-assignment job (`AutoAssignTicketJob`) skips agents whose `AvailabilityStatus != Available`.

**BR-AGT-009** Agent changes their own status via `PUT /agents/me/availability`. Managers and Admins can change any agent's status via `PUT /admin/users/{id}/availability` (implicit in user management).

**BR-AGT-010** When an agent sets status to `Offline` or `Away`, their existing assigned tickets are NOT automatically reassigned. The tickets remain with that agent. Managers must manually reassign if needed.

**BR-AGT-011** Availability status is included in `GET /dashboard/kpis → agentWorkload[]` for management visibility.

---

## Quick Reply Templates

Templates are pre-written reply drafts agents can insert into ticket replies.

### Scope

| Scope | Created By | Visible To |
|-------|-----------|-----------|
| `Personal` | Agent themselves | That agent only |
| `Global` | Admin | All agents |

**BR-AGT-012** Agents can create, edit, and delete their own `Personal` scope templates. They cannot edit or delete `Global` templates.

**BR-AGT-013** `GET /agents/me/templates` returns both Personal templates (owned by the caller) and all Global templates.

### Template Variables

Templates support `{{variable}}` tokens:
- `{{customer_name}}` — ticket's customer full name
- `{{agent_name}}` — caller's full name
- `{{ticket_number}}` — ticket's TicketNumber
- `{{department}}` — ticket's Department name

`POST /agents/me/templates/{id}/render` accepts a `ticketId` and returns the template with all variables substituted using that ticket's data.

**BR-AGT-014** Rendering a template with an invalid or inaccessible `ticketId` returns `404`.

**BR-AGT-015** Unknown `{{tokens}}` not in the supported list are left as-is (not errored) — agents may use their own informal placeholders that they manually replace before sending.

---

## Personal Tasks

Personal tasks are private to-do items visible only to the owning agent. They are not linked to tickets (though agents may note a ticket number in the title).

**BR-AGT-016** Tasks have: `title` (required), `description` (optional), `dueDate` (optional), `isCompleted` (bool, default false).

**BR-AGT-017** `GET /agents/me/tasks` lists the caller's tasks only. Sort: incomplete first, then by `dueDate ASC`, then by `createdAt ASC`.

**BR-AGT-018** Completed tasks are retained for 30 days then purged by a nightly Hangfire job.

**BR-AGT-019** Maximum 200 active (incomplete) tasks per agent. Adding beyond this returns `422` with code `MAX_TASKS_REACHED`.

---

## Acceptance Criteria

**AC-AGT-001** Given Agent A has 5 assigned tickets and calls `GET /agents/me/tickets`, then exactly 5 tickets are returned — none belonging to Agent B.

**AC-AGT-002** Given a ticket with Critical priority and 5 minutes remaining on SLA, when the agent's dashboard loads, then that ticket appears first in the default sort order.

**AC-AGT-003** Given Agent A sets status to Busy, when a new ticket is auto-assigned, then Agent A is skipped and the next Available agent is assigned.

**AC-AGT-004** Given Agent A renders template `"Hello {{customer_name}}, your ticket {{ticket_number}} is being processed."` with `ticketId` for Customer Sara's ticket TKT-2025-00043, then the result is `"Hello Sara Al-Mansouri, your ticket TKT-2025-00043 is being processed."`.

**AC-AGT-005** Given an Agent tries to DELETE a Global template, then the response is `403 Forbidden`.

**AC-AGT-006** Given `GET /tickets/unassigned` called by an Agent in Department A, then only unassigned tickets from Department A are returned (not Department B tickets).

**AC-AGT-007** Given two agents simultaneously call `POST /tickets/{id}/assign` on the same unassigned ticket, then exactly one succeeds and the other receives `409 Conflict` with code `TICKET_ALREADY_ASSIGNED`.

**AC-AGT-008** Given an Agent has 200 incomplete tasks, when they try to create a 201st, then the response is `422` with code `MAX_TASKS_REACHED`.

---

## Edge Cases

- **Agent in multiple departments**: `GET /agents/me/tickets` shows all their assigned tickets regardless of department. `GET /tickets/unassigned` shows unassigned tickets from all their departments combined.
- **Template with all unknown tokens**: the rendered template is returned unchanged. No error.
- **Task with past due date**: allowed — agents may backlog tasks. No validation on `dueDate` past/future.
- **Ticket status change while viewing**: agent's dashboard shows a snapshot. Real-time updates pushed via SignalR `ReceiveNotification` — client must refresh the ticket item on receiving a `TicketStatusChanged` notification for a ticket in the agent's list.

---

## Integration Points

| Triggers | Downstream Effect |
|---------|-------------------|
| Agent sets `Available` | Auto-assignment job may immediately assign pending unassigned tickets |
| Agent posts `TicketMessage` | Communication module sends outbound message, Notifications module alerts customer |
| Agent creates/renders template | No downstream events (pure UI utility) |
