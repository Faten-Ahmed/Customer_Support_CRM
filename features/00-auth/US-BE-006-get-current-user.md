# US-BE-006 — Get Current User (me)

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
**As a** logged-in user, **I want to** fetch my own profile from the token, **so that** the client can display my name, role, and settings.

## Acceptance Criteria
- [ ] `GET /auth/me` returns `200` with the caller's full profile (id, fullName, email, role, primaryDepartment, departments[], skills[], availabilityStatus, isActive)
- [ ] Returns `401` if no valid Bearer token is present
- [ ] Deactivated user's token returns `401` with code `ACCOUNT_INACTIVE`

## Technical Notes
- Endpoint: `GET /auth/me`
- Entity: `User` with `AgentDepartment[]`, `AgentSkill[]`
- Spec: `specs/api/auth.md`

## Dependencies
- US-BE-001
