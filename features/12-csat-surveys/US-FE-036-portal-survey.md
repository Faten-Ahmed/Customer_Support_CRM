# US-FE-036 — Portal Survey Page

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
**As a** customer, **I want to** complete a satisfaction survey after my ticket is resolved, **so that** I can give feedback on my experience.

## Acceptance Criteria
- [ ] Route: `/portal/surveys/{id}`
- [ ] Shows ticket number and subject for context
- [ ] Star rating selector (1–5, visual stars, required)
- [ ] Optional comment textarea (max 1000 chars with counter)
- [ ] Submit button; loading spinner on submit
- [ ] On `422 SURVEY_EXPIRED`: shows "This survey has expired" with no form
- [ ] On `422 SURVEY_ALREADY_SUBMITTED`: shows "Thank you — you already submitted feedback"
- [ ] On success: thank-you message with link to "View my tickets"

## Technical Notes
- Component: `PortalSurveyComponent`
- Service: `PortalSurveyService.get()`, `PortalSurveyService.submit()`
- Spec: `specs/api/customer-portal.md`

## Dependencies
- US-BE-082, US-FE-032
