# US-BE-080 — Portal Profile Get / Update

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
**As a** customer, **I want to** view and update my profile, **so that** my contact details are accurate.

## Acceptance Criteria
- [ ] `GET /portal/profile` returns: `id`, `fullName`, `email`, `phone`, `companyName`, `country`, `city`
- [ ] `PUT /portal/profile` accepts `fullName`, `phone`, `city` (partial update); email and companyName are ignored if supplied
- [ ] Returns `200` with updated profile
- [ ] Non-customer role calling portal endpoints returns `403`

## Technical Notes
- Endpoints: `GET /portal/profile`, `PUT /portal/profile`
- Entity: `Customer`
- Business rules: BR-PLT-005, BR-PLT-006
- Spec: `specs/api/customer-portal.md`

## Dependencies
- US-BE-010, US-BE-007
