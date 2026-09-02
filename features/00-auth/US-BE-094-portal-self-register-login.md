# US-BE-094 — Portal Customer Login

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
**As a** registered customer, **I want to** log in to the portal, **so that** I can view and manage my tickets.

## Acceptance Criteria
- [ ] `POST /auth/login` with valid customer `email` + `password` issues JWT with `role = Customer`
- [ ] `EmailVerified = false` customer returns `401` with code `EMAIL_NOT_VERIFIED`
- [ ] `IsActive = false` customer returns `401` with code `ACCOUNT_INACTIVE`
- [ ] Customer JWT grants access only to `/portal/*` endpoints; calling `/tickets` (agent endpoint) returns `403`

## Technical Notes
- Endpoint: `POST /auth/login` (same endpoint, role differentiated by entity type)
- Entity: `Customer` (separate from `User` for internal staff)
- Business rules: BR-PLT-001, BR-PLT-002
- Spec: `specs/api/auth.md`, `specs/features/09-customer-portal.md`

## Dependencies
- US-BE-010, US-BE-011, US-BE-007
