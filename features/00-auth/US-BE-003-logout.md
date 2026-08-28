# US-BE-003 — Logout

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
**As a** logged-in user, **I want to** log out, **so that** my session is invalidated and the refresh token cannot be reused.

## Acceptance Criteria
- [ ] `POST /auth/logout` revokes the current refresh token (marks `isRevoked = true`)
- [ ] Response clears the `refreshToken` HttpOnly cookie (Set-Cookie with past expiry)
- [ ] Returns `200` with `{ "data": { "loggedOut": true } }`
- [ ] Calling logout with no refresh token cookie still returns `200` (idempotent)

## Technical Notes
- Endpoint: `POST /auth/logout`
- Entity: `RefreshToken`
- Spec: `specs/api/auth.md`

## Dependencies
- US-BE-002
