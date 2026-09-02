# US-BE-068 — CRUD Branches

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
**As an** admin, **I want to** manage branches, **so that** customers and tickets can be grouped by location for reporting.

## Acceptance Criteria
- [ ] `GET /admin/branches` returns all branches
- [ ] `POST /admin/branches` with `name` (required), optional `nameAr`; returns `201`
- [ ] `PUT /admin/branches/{id}` updates name, nameAr; returns `200`
- [ ] `POST /admin/branches/{id}/deactivate` and `/reactivate` toggle `isActive`
- [ ] Branches have no SLA or routing impact — informational only (BR-ADM-014)

## Technical Notes
- Endpoints: CRUD on `/admin/branches`
- Entity: `Branch`
- Business rules: BR-ADM-014, BR-ADM-015
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-007
