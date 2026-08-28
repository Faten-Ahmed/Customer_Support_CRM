# US-BE-011 — Email Verification

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
**Roles:** Anonymous (token-based)
**As a** self-registered customer, **I want to** verify my email by clicking a link, **so that** my portal account is activated.

## Acceptance Criteria
- [ ] `POST /auth/portal/verify-email` with `{ "token": "..." }` activates the customer (`EmailVerified = true`, `IsActive = true`) and returns `200`
- [ ] Expired token (> 24h) returns `422` with code `TOKEN_EXPIRED`
- [ ] Already-used or invalid token returns `422` with code `INVALID_TOKEN`
- [ ] After verification, the customer can log in via `POST /auth/login`
- [ ] Re-registering with the same email before verification resets the token and re-sends the email

## Technical Notes
- Endpoint: `POST /auth/portal/verify-email`
- Entity: `Customer`, `EmailVerificationToken`
- Business rule: BR-CUST-005
- Spec: `specs/api/auth.md`

## Dependencies
- US-BE-010
