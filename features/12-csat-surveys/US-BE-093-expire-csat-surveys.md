# US-BE-093 — Expire CSAT Surveys Job

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
**Roles:** System (Hangfire recurring job)
**As the** system, **I want to** mark unanswered surveys as expired after 7 days, **so that** reporting reflects true response rates.

## Acceptance Criteria
- [ ] `ExpireCsatSurveysJob` runs daily at 00:05 UTC (Hangfire cron: `5 0 * * *`)
- [ ] Finds all `CsatSurvey` where `Status = Sent` and `SentAt < now - 7 days`
- [ ] Sets `Status = Expired` in batch update
- [ ] Expired surveys count toward `totalSent` in CSAT report but NOT toward `avgRating` or `totalSubmitted`

## Technical Notes
- Implementation: Hangfire recurring job
- Entity: `CsatSurvey`
- Business rule: BR-CSAT-004
- Spec: `specs/features/12-csat-surveys.md`

## Dependencies
- US-BE-092
