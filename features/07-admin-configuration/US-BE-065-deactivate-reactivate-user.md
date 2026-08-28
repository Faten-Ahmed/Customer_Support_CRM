# US-BE-065 — Deactivate / Reactivate User

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


**Epic:** Admin Configuration
**Roles:** Admin
**As an** admin, **I want to** deactivate a user who leaves the company, **so that** they can no longer log in.

## Acceptance Criteria
- [ ] `POST /admin/users/{id}/deactivate` sets `IsActive = false`; returns `{ "id", "isActive": false }`
- [ ] Cannot deactivate self; returns `422` with code `CANNOT_DEACTIVATE_SELF`
- [ ] Cannot deactivate the last active Admin; returns `422` with code `CANNOT_DEACTIVATE_LAST_ADMIN`
- [ ] On deactivation: all user's refresh tokens revoked (force immediate logout)
- [ ] `POST /admin/users/{id}/reactivate` sets `IsActive = true`; returns `{ "id", "isActive": true }`

## Technical Notes
- Endpoints: `POST /admin/users/{id}/deactivate`, `POST /admin/users/{id}/reactivate`
- Entity: `User`, `RefreshToken`
- Business rules: BR-ADM-004, BR-ADM-005, BR-ADM-006
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-063, US-BE-007
