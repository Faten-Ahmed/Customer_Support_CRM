# US-BE-076 — CSAT Report

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


**Epic:** Reports & Dashboard
**Roles:** Admin, Manager, Agent
**As a** manager, **I want to** see CSAT scores and response distribution, **so that** I can track customer satisfaction trends.

## Acceptance Criteria
- [ ] `GET /reports/csat` returns overall (avgRating, totalSent, totalSubmitted, responseRate), distribution (1–5 counts), byDepartment[], byAgent[], recentComments (last 20)
- [ ] `avgRating = null` when `totalSubmitted = 0`
- [ ] Expired surveys count toward `totalSent` but NOT `totalSubmitted` or `avgRating`
- [ ] `responseRate = (totalSubmitted / totalSent) × 100`
- [ ] Role scoping same as other reports

## Technical Notes
- Endpoint: `GET /reports/csat`
- Entity: `CsatSurvey`
- Business rules: BR-RPT-010—012, BR-CSAT-010—012
- Spec: `specs/api/reports.md`

## Dependencies
- US-BE-111, US-BE-007
