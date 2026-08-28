# US-BE-002 — Token Refresh

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
**Roles:** Any authenticated user
**As a** logged-in user, **I want my** session to silently renew, **so that** I am not forced to log in again every 15 minutes.

## Acceptance Criteria
- [ ] `POST /auth/refresh` reads the `refreshToken` HttpOnly cookie; returns a new `accessToken` and rotates the refresh token cookie
- [ ] Expired or invalid refresh token returns `401` with code `INVALID_REFRESH_TOKEN`
- [ ] Refresh token can only be used once (rotation — old token is invalidated on use)
- [ ] Refresh token issued to a deactivated user returns `401`

## Technical Notes
- Endpoint: `POST /auth/refresh`
- Entity: `RefreshToken` (token hash, userId, expiresAt, isRevoked)
- Spec: `specs/api/auth.md`

## Dependencies
- US-BE-001 (login issues the initial refresh token)
