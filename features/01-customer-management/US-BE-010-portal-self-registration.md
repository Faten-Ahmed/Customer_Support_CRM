# US-BE-010 — Portal Self-Registration

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


**Epic:** Customer Management
**Roles:** Anonymous (public)
**As a** new customer, **I want to** register on the portal, **so that** I can submit and track support tickets.

## Acceptance Criteria
- [ ] `POST /auth/portal/register` with `fullName`, `email`, `password` returns `202 Accepted`
- [ ] Customer record created with `IsActive = false`, `EmailVerified = false`
- [ ] `SendVerificationEmailJob` enqueued (Hangfire): email contains a 24-hour token link
- [ ] Duplicate email returns `409` with code `EMAIL_ALREADY_EXISTS`
- [ ] No JWT is issued at registration — customer cannot log in until verified
- [ ] Password complexity enforced (same rules as US-BE-005)

## Technical Notes
- Endpoint: `POST /auth/portal/register`
- Entity: `Customer`, `EmailVerificationToken`
- Business rules: BR-CUST-003, BR-CUST-004
- Spec: `specs/api/auth.md`, `specs/features/01-customer-management.md` W-CUST-02

## Dependencies
- None (public endpoint)
