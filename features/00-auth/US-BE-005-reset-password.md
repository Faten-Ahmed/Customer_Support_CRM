# US-BE-005 — Reset Password

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


**Epic:** Authentication
**Roles:** Any user
**As a** user with a reset token, **I want to** set a new password, **so that** I can log in again.

## Acceptance Criteria
- [ ] `POST /auth/reset-password` with valid `token` + `newPassword` updates `User.PasswordHash` and marks the token as used (`isUsed = true`)
- [ ] Returns `200` with `{ "data": { "passwordReset": true } }`
- [ ] Expired token (> 1 hour old) returns `422` with code `TOKEN_EXPIRED`
- [ ] Already-used token returns `422` with code `TOKEN_ALREADY_USED`
- [ ] Unknown token returns `422` with code `INVALID_TOKEN`
- [ ] After reset: all existing refresh tokens for that user are revoked (force re-login)
- [ ] Password must meet complexity rules: min 8 chars, at least 1 uppercase, 1 digit; returns `422` with code `WEAK_PASSWORD` if not met

## Technical Notes
- Endpoint: `POST /auth/reset-password`
- Entities: `PasswordResetToken`, `User`, `RefreshToken`
- Spec: `specs/api/auth.md`

## Dependencies
- US-BE-004
