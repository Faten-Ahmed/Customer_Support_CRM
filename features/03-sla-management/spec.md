# Feature Spec — SLA Management

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


> Requirements: `REQ-SLA-*`
> API: `specs/api/tickets.md` (GET /{id}/sla), `specs/api/admin.md` (SLA Policies), `specs/api/reports.md` (GET /reports/sla)
> Domain entities: `SlaPolicy`, `TicketSla`, `BusinessHours`, `Holiday`

---

## Overview

SLA (Service Level Agreement) management tracks response and resolution time commitments per ticket priority. Time is measured in business hours only — nights, weekends, and holidays are paused. Three breach tiers trigger escalating alerts. SLA is a read-only computed concern for agents; only Admins configure policies.

---

## SLA Policy Structure

One `SlaPolicy` row per `Priority` × `Department` combination. If no Department-specific policy exists, the global policy for that priority applies.

| Priority | Default First Response | Default Resolution | Update Frequency |
|----------|----------------------|-------------------|-----------------|
| Critical | 15 min | 4 hours | 30 min |
| High | 2 hours | 8 hours | 60 min |
| Medium | 8 hours | 24 hours | 120 min |
| Low | 24 hours | 72 hours | 240 min |

All durations are in **business minutes** (i.e., clock pauses outside business hours).

---

## Business Hours Calculation

**BR-SLA-001** Business hours are defined per Department. If a Department has no override, the global business hours apply.

**BR-SLA-002** Business days: configurable per the `BusinessHours` record (e.g., Sunday–Thursday for the GCC market).

**BR-SLA-003** Business time window: `startTime`–`endTime` in the configured `timeZone`.

**BR-SLA-004** `Holiday` entries in the `BusinessHours` record are excluded from business-day calculations (the entire day counts as non-business).

**BR-SLA-005** All SLA deadline calculations are performed in UTC internally. Display to agents converts to the Department's configured time zone.

### Business Minute Calculation Algorithm

```
function addBusinessMinutes(startUtc, minutes, businessHours):
  remaining = minutes
  current = startUtc
  while remaining > 0:
    if isBusinessTime(current, businessHours):
      minutesToEndOfDay = minutesUntilEndOfBusinessDay(current, businessHours)
      if remaining <= minutesToEndOfDay:
        return current + remaining minutes
      else:
        remaining -= minutesToEndOfDay
        current = startOfNextBusinessDay(current, businessHours)
    else:
      current = startOfNextBusinessDay(current, businessHours)
  return current
```

---

## SLA Clock Lifecycle

### Clock States

| State | Trigger |
|-------|---------|
| **Running** | Ticket created (first response clock starts immediately) |
| **Paused** | `Ticket.Status = OnHold` |
| **Resumed** | `Ticket.Status` transitions from `OnHold` to `InProgress` |
| **Stopped (Met)** | First customer-facing agent reply posted (first response clock only) / Ticket Resolved (resolution clock) |
| **Stopped (Breached)** | Clock expired without the corresponding SLA event |

**BR-SLA-006** Two independent clocks run per ticket:
1. **First Response Clock** — starts at ticket creation; stops when any Agent posts a customer-facing message (`IsInternal = false`).
2. **Resolution Clock** — starts at ticket creation; stops when `Status = Resolved`.

Both clocks are paused together when `Status = OnHold`.

**BR-SLA-007** After ticket transfer to a new Department: if the new Department has a different SLA policy, both deadlines are recalculated from the point of transfer using the elapsed business time already consumed.

---

## Breach Tiers

| Tier | Threshold | Action |
|------|-----------|--------|
| **Warning** | 80% of deadline elapsed | Push notification to assigned agent |
| **Breach** | 100% of deadline elapsed | Notification to agent + department manager; `BreachLevel = Breach` recorded |
| **CriticalBreach** | 200% of deadline elapsed | Notification to agent + manager + escalation to Admin; `BreachLevel = CriticalBreach` recorded; ticket auto-escalated if not already |

Breach level is stored on `TicketSla.FirstResponseBreachLevel` and `TicketSla.ResolutionBreachLevel`. Once set, the breach level is never downgraded (even if the action is eventually completed).

---

## SLA Monitoring Job

A Hangfire recurring job `SlaMonitorJob` runs every **5 minutes**:

1. Query all open tickets (Status not in `Closed`, `Resolved`) that have not had their first response.
2. For each: compute elapsed business minutes. Check if Warning/Breach/CriticalBreach threshold crossed since last run.
3. If a new threshold is crossed:
   a. Update `TicketSla.FirstResponseBreachLevel` or `ResolutionBreachLevel`.
   b. Create `Notification` records for the appropriate recipients.
   c. Push via SignalR.
   d. For `CriticalBreach` on Resolution SLA: if ticket is not already `Escalated`, auto-escalate (same as `POST /tickets/{id}/escalate` with `reason = "SLA Critical Breach — auto-escalated"`).

**BR-SLA-008** Notification deduplication: each breach tier triggers at most one notification per ticket per tier. Sending the same tier notification twice is prevented by checking if a notification for that ticket+tier already exists.

---

## TicketSla Record

Created atomically with the ticket:

```
TicketSla {
  TicketId             -- FK
  SlaPolicy snapshot at creation time:
    FirstResponseMinutes
    ResolutionMinutes
    UpdateFrequencyMinutes
    WarningThresholdPercent
  Calculated deadlines:
    FirstResponseDeadlineUtc
    ResolutionDeadlineUtc
  Running state:
    PausedAt             -- null if running
    TotalPausedMinutes   -- accumulated pause time
  Met timestamps:
    FirstRespondedAt     -- null until met
    ResolvedAt           -- null until resolved
  Breach levels:
    FirstResponseBreachLevel   -- null | Warning | Breach | CriticalBreach
    ResolutionBreachLevel      -- null | Warning | Breach | CriticalBreach
}
```

**BR-SLA-009** The SLA policy values are snapshotted at ticket creation. Changing the SLA policy afterward does not retroactively affect open tickets.

---

## GET /tickets/{id}/sla Response Details

Returns computed, always-fresh values (not just stored fields):

- `elapsedBusinessMinutes` — recomputed on each call based on current time minus business-hour pauses
- `remainingBusinessMinutes` — deadline minus elapsed
- `percentElapsed` — for both clocks
- `currentBreachLevel` — live tier (may show Warning even if breach hasn't been persisted yet by the monitoring job)

---

## Acceptance Criteria

**AC-SLA-001** Given a Critical ticket created on a Thursday at 17:55 (business hours end at 18:00), when the first response deadline of 15 business minutes is calculated, then `FirstResponseDeadlineUtc` falls at 08:05 on the next business day (carry-over).

**AC-SLA-002** Given a ticket is placed OnHold at 10:00 and resumed at 14:00, when elapsed business minutes are computed, then the 4-hour OnHold gap is excluded.

**AC-SLA-003** Given a ticket has consumed 80% of its resolution SLA, when the SlaMonitorJob runs, then a Warning notification is sent to the assigned agent and `ResolutionBreachLevel = Warning` is stored.

**AC-SLA-004** Given the same ticket later crosses 100%, when the SlaMonitorJob runs, then `ResolutionBreachLevel = Breach` (not Warning again — the new tier is set), and the manager also receives a notification.

**AC-SLA-005** Given an SLA Policy is updated (e.g., Critical resolution time changed from 240 to 180 minutes), when viewing an open ticket created before the change, then `GET /tickets/{id}/sla` still shows the original 240-minute policy values (snapshot isolation).

**AC-SLA-006** Given a ticket is transferred to a Department with a different SLA policy (longer resolution time), when the new deadline is calculated, then already-elapsed business minutes are subtracted from the new policy's limit.

**AC-SLA-007** Given a ticket reaches 200% of its resolution SLA and is not Escalated, when the SlaMonitorJob runs, then the ticket Status is changed to Escalated with reason "SLA Critical Breach — auto-escalated".

**AC-SLA-008** Given a national holiday is configured for Dec 1, when a ticket created on Nov 30 at 17:50 has its deadline calculated, then Dec 1 is skipped and deadline lands on Dec 2.

---

## Edge Cases

- **Ticket created outside business hours** (e.g., 02:00 AM): the SLA clock starts running but all time until the next business day opens is non-business — effectively the clock doesn't advance until business opens.
- **OnHold with no resume**: if a ticket stays OnHold indefinitely, the SLA clock is frozen. The monitoring job skips OnHold tickets for breach alerting since they are intentionally paused.
- **Rapid consecutive status changes**: if a ticket transitions OnHold → InProgress → OnHold within seconds, `TotalPausedMinutes` accumulates each pause segment correctly.
- **Zero-minute SLA**: not allowed — policy values must be ≥ 1 minute (enforced at admin API validation).

---

## Integration Points

| Event Published | Consumed By |
|----------------|-------------|
| `SlaWarningTriggered` | Notifications, Dashboard |
| `SlaBreached` | Notifications, Dashboard, Reports |
| `SlaCriticalBreachTriggered` | Notifications, Ticket (auto-escalate), Reports |
| `TicketOnHold` | SLA (pause clock) |
| `TicketStatusChanged` | SLA (resume or stop clock) |
| `TicketTransferred` | SLA (recalculate deadlines) |
