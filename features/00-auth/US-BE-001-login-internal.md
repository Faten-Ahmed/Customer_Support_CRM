# US-BE-001 — Login (Internal Users)

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
**Roles:** Admin, Manager, Agent
**As a** support staff member, **I want to** log in with my email and password, **so that** I can access the CRM with an appropriate JWT.

## Acceptance Criteria
- [ ] `POST /auth/login` with valid `email` + `password` returns `200` with `accessToken` (JWT, 15-min TTL) and sets `refreshToken` as HttpOnly cookie (7-day TTL)
- [ ] Invalid password or unknown email returns `401` with code `INVALID_CREDENTIALS`
- [ ] `IsActive = false` user returns `401` with code `ACCOUNT_INACTIVE`
- [ ] `PasswordMustChange = true` user is issued a token but all subsequent API calls return `423` with code `PASSWORD_CHANGE_REQUIRED` (except `POST /auth/change-password`)
- [ ] JWT payload contains: `sub` (userId), `role`, `email`, `name`, `departmentId` (primaryDept), `jti`
- [ ] Passwords are hashed with BCrypt (cost factor ≥ 12); plaintext is never stored or logged

## Technical Notes
- Endpoint: `POST /auth/login`
- Entity: `User` (`Email`, `PasswordHash`, `IsActive`, `PasswordMustChange`)
- Spec: `specs/api/auth.md`

## Dependencies
- None — foundational story
