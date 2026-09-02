# US-BE-008 — First Login Password Change Enforcement

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
**Roles:** Admin, Manager, Agent (internally created users)
**As a** system admin, **I want** newly created users to be forced to change their temporary password on first login, **so that** temp passwords don't persist as a security risk.

## Acceptance Criteria
- [ ] User created via `POST /admin/users` is given `PasswordMustChange = true`
- [ ] `POST /auth/change-password` accepts `currentPassword` + `newPassword`; on success sets `PasswordMustChange = false`
- [ ] Until `PasswordMustChange = false`, all endpoints except `POST /auth/change-password` and `POST /auth/logout` return `423 Locked` with code `PASSWORD_CHANGE_REQUIRED`
- [ ] `newPassword` must pass complexity rules (same as US-BE-005)
- [ ] `currentPassword` must match the stored hash; wrong password returns `422` with code `INVALID_CURRENT_PASSWORD`

## Technical Notes
- Endpoint: `POST /auth/change-password`
- Entity: `User.PasswordMustChange`, `User.PasswordHash`
- Business rule: BR-ADM-009
- Spec: `specs/api/admin.md` (user creation), `specs/api/auth.md`

## Dependencies
- US-BE-001, US-BE-007
