# US-BE-082 — Portal Survey Get & Submit

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


**Epic:** Customer Portal
**Roles:** Customer
**As a** customer, **I want to** rate my support experience, **so that** the company can measure satisfaction.

## Acceptance Criteria
- [ ] `GET /portal/surveys/{id}` returns survey with `ticketNumber`, `ticketSubject`, `sentAt`, `isExpired`; `403` if belongs to another customer
- [ ] `POST /portal/surveys/{id}/submit` with `rating` (1–5 int, required) and optional `comment` (max 1000 chars) submits the survey
- [ ] Expired survey (> 7 days) returns `422` with code `SURVEY_EXPIRED`
- [ ] Already submitted returns `422` with code `SURVEY_ALREADY_SUBMITTED`
- [ ] `rating` out of range [1,5] returns `422` with code `INVALID_RATING`
- [ ] `CsatSubmitted` event published → Dashboard KPI updated

## Technical Notes
- Endpoints: `GET /portal/surveys/{id}`, `POST /portal/surveys/{id}/submit`
- Entity: `CsatSurvey`
- Business rules: BR-PLT-024—028, BR-CSAT-005—008
- Spec: `specs/api/customer-portal.md`

## Dependencies
- US-BE-111, US-BE-007
