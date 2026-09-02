# US-BE-092 — Auto-Trigger CSAT Survey on Ticket Close

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


**Epic:** CSAT Surveys
**Roles:** System
**As the** system, **I want to** automatically send a CSAT survey when a ticket is closed, **so that** we capture customer satisfaction without manual effort.

## Acceptance Criteria
- [ ] On `TicketClosed` event: `SendCsatSurveyJob` is enqueued (Hangfire)
- [ ] Job creates `CsatSurvey` with `Status = Sent`, `SentAt = now`, `AgentId` (at time of close), `DepartmentId` (at time of close) snapshotted
- [ ] Unique constraint: only one `CsatSurvey` per ticket; if one already exists, skip creation
- [ ] `SurveyAvailable` notification sent to customer (in-app)
- [ ] Survey email sent if customer has `EmailVerified = true`

## Technical Notes
- Implementation: Hangfire `SendCsatSurveyJob`, triggered by `TicketClosed` event
- Entity: `CsatSurvey`, `Notification`
- Business rules: BR-CSAT-001—003, BR-CSAT-013, BR-CSAT-015, BR-CSAT-016
- Spec: `specs/features/12-csat-surveys.md`

## Dependencies
- US-BE-025, US-BE-053, US-BE-054
