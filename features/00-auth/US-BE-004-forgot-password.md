# US-BE-004 — Forgot Password

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
**Roles:** Any user (internal + customer)
**As a** user who forgot my password, **I want to** request a reset link, **so that** I can regain access to my account.

## Acceptance Criteria
- [ ] `POST /auth/forgot-password` with a valid registered `email` enqueues a `SendPasswordResetEmailJob` (Hangfire) and returns `202`
- [ ] Unknown email returns `202` as well (no enumeration — same response regardless)
- [ ] Reset token is a cryptographically secure random string, stored hashed in DB with 1-hour TTL
- [ ] The emailed link format: `{baseUrl}/reset-password?token={raw-token}`
- [ ] Only one active reset token per user — issuing a new one invalidates the previous

## Technical Notes
- Endpoint: `POST /auth/forgot-password`
- Entity: `PasswordResetToken` (tokenHash, userId, expiresAt, isUsed)
- Spec: `specs/api/auth.md`

## Dependencies
- US-BE-001
