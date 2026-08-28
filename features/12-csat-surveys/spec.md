# Feature Spec — CSAT Surveys

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


> Requirements: `REQ-CSAT-*`
> API: `specs/api/customer-portal.md` (GET/POST /portal/surveys), `specs/api/reports.md` (GET /reports/csat)
> Domain entities: `CsatSurvey`, `CsatResponse`

---

## Overview

Customer Satisfaction (CSAT) surveys are automatically triggered when a ticket is closed. A 1–5 numeric rating is collected, with an optional free-text comment. Survey data feeds into agent performance reports, department scores, and the management dashboard KPI. Only one survey is sent per ticket.

---

## Survey Lifecycle

```
[Not Sent] ──ticket closed──▶ [Sent] ──customer submits──▶ [Submitted]
                                          │
                               (7 days pass without submit)
                                          ▼
                                      [Expired]
```

| Status | Description |
|--------|-------------|
| `Sent` | Survey dispatched, awaiting response |
| `Submitted` | Customer has completed the survey |
| `Expired` | 7 days elapsed without submission |

---

## Business Rules

**BR-CSAT-001** A CSAT survey is triggered automatically when a `Ticket.Status` transitions to `Closed`, regardless of who closed it (customer, agent, or auto-close job).

**BR-CSAT-002** Exactly one `CsatSurvey` record is created per ticket. If a ticket is closed, reopened, and closed again, no second survey is created (the first survey record is reused; if already Submitted, no additional survey is sent).

**BR-CSAT-003** The survey is dispatched asynchronously via a Hangfire job `SendCsatSurveyJob`, triggered by the `TicketClosed` domain event. The job:
  1. Creates a `CsatSurvey` record with `Status = Sent`, `SentAt = now`.
  2. Sends a `SurveyAvailable` notification to the customer (in-app via NotificationHub).
  3. Sends an email to the customer (if they have a portal account with verified email) containing the survey link: `{portalBaseUrl}/surveys/{surveyId}`.

**BR-CSAT-004** Survey expiry: a Hangfire scheduled job `ExpireCsatSurveysJob` runs daily at 00:05 UTC. It marks `Status = Expired` for all surveys where `Status = Sent` AND `SentAt < now - 7 days`.

**BR-CSAT-005** A submitted survey cannot be re-submitted or edited. `POST /portal/surveys/{id}/submit` after `Status = Submitted` returns `422` with code `SURVEY_ALREADY_SUBMITTED`.

**BR-CSAT-006** An expired survey cannot be submitted. `POST /portal/surveys/{id}/submit` after `Status = Expired` returns `422` with code `SURVEY_EXPIRED`.

**BR-CSAT-007** `rating` must be an integer in range [1, 5] inclusive. Non-integer, null, or out-of-range values return `422` with code `INVALID_RATING`.

**BR-CSAT-008** `comment` is optional, max 1000 characters. If provided, it is stored verbatim (no sanitisation beyond XSS stripping; stored as plain text, not HTML).

**BR-CSAT-009** A Customer may only access surveys belonging to their own tickets. `GET /portal/surveys/{id}` or `POST /portal/surveys/{id}/submit` for another customer's survey returns `403`.

---

## CsatSurvey Record

```
CsatSurvey {
  Id                  -- PK
  TicketId            -- FK (unique — one per ticket)
  CustomerId          -- FK (denormalised for fast lookup)
  Status              -- Sent | Submitted | Expired
  SentAt              -- when survey was dispatched
  SubmittedAt         -- null until submitted
  Rating              -- null until submitted (1–5)
  Comment             -- null until submitted
}
```

---

## CSAT Score Calculation

**BR-CSAT-010** CSAT score for reporting purposes is the simple arithmetic mean of all submitted `Rating` values in the period/scope. Example: ratings [5, 4, 5, 3] → score = 4.25.

**BR-CSAT-011** `csatScore` in reports is null (not 0) when there are zero submitted surveys in the scope. A 0 score would be misleading.

**BR-CSAT-012** Response rate = `(totalSubmitted / totalSent) × 100`. Expired surveys count as sent-but-not-submitted for this calculation.

**BR-CSAT-013** CSAT data is attributed to:
  - The `AssignedAgentId` at the time of ticket closure (for per-agent reporting)
  - The `DepartmentId` at the time of ticket closure (for per-department reporting)
  - These are stored on the `CsatSurvey` record at creation time to prevent attribution drift if the ticket is later transferred.

---

## Real-Time Dashboard Integration

**BR-CSAT-014** When a survey is submitted, the domain event `CsatSubmitted` is published. This triggers:
  - DashboardHub `KpiUpdated` push (rolling 30-day `csatScore` recalculated)
  - Report cache invalidation (if any caching layer exists for CSAT aggregates)

---

## Email Survey Notification

**BR-CSAT-015** The survey email contains:
  - Subject: `How did we do? — Ticket {TicketNumber}`
  - Body: plain text + direct link `{portalBaseUrl}/surveys/{surveyId}`
  - Unsubscribe link: not required in v1 (internal enterprise use, not marketing)

**BR-CSAT-016** Survey emails are only sent to customers with `EmailVerified = true`. Customers created internally by agents without portal accounts do not receive the email (they have no email access to click the link). They do receive the in-app notification when they register for the portal.

---

## Acceptance Criteria

**AC-CSAT-001** Given a ticket transitions to Closed, then exactly one CsatSurvey record is created with Status = Sent and SentAt = now (within 5 seconds of closure).

**AC-CSAT-002** Given a ticket is closed, reopened (by customer reply), and closed again, then no second CsatSurvey record is created.

**AC-CSAT-003** Given a survey was sent 8 days ago and ExpireCsatSurveysJob runs, then the survey Status becomes Expired.

**AC-CSAT-004** Given Customer A tries to access the survey URL for Customer B's ticket, then the response is `403 Forbidden`.

**AC-CSAT-005** Given `POST /portal/surveys/{id}/submit` with `rating = 6`, then the response is `422` with code `INVALID_RATING`.

**AC-CSAT-006** Given `POST /portal/surveys/{id}/submit` with `rating = 5` and `comment` of 1001 characters, then the response is `422` (comment too long).

**AC-CSAT-007** Given a survey is submitted, when `GET /reports/csat` is called, then `totalSubmitted` increases by 1 and `avgRating` reflects the new submission.

**AC-CSAT-008** Given `GET /reports/csat` with zero submitted surveys, then `avgRating = null` (not 0).

**AC-CSAT-009** Given a ticket is closed by auto-close job (not the customer), then a CSAT survey is still created and the customer is notified.

**AC-CSAT-010** Given `csatScore` in the CSAT report for Agent A is computed from 3 submitted surveys with ratings [4, 5, 3], then `csatScore = 4.0` (rounded to 1 decimal place).

---

## Edge Cases

- **Ticket with no customer email**: survey is created (for in-app notification) but no email is sent. If the customer later verifies their email, they can still access the survey link shared in their in-app notification (if not yet expired).
- **Survey submitted after ticket reopened**: logically impossible by the state machine — once a ticket is closed and a survey is submitted, reopening the ticket does not un-submit the survey.
- **Bulk ticket close (admin action)**: if an admin closes 50 tickets at once, 50 separate `SendCsatSurveyJob` jobs are enqueued. They execute independently. No bulk email batching in v1.
- **Duplicate SendCsatSurveyJob enqueue**: the `BR-CSAT-002` uniqueness constraint (one survey per ticket) prevents duplicate survey records even if the job is accidentally queued twice.

---

## Integration Points

| Event Published | Consumed By |
|----------------|-------------|
| `CsatSurveySent` | Notifications (customer: `SurveyAvailable`) |
| `CsatSubmitted` | Reports (CSAT aggregates), Dashboard (`KpiUpdated` push) |

| Event Consumed | Source |
|----------------|--------|
| `TicketClosed` | Ticket Management (triggers survey creation job) |
