# US-FE-027 — User Management (Admin)

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
**As an** admin, **I want to** manage staff accounts from a single page, **so that** onboarding and offboarding are straightforward.

## Acceptance Criteria
- [ ] Route: `/admin/users`
- [ ] Table: Name, Email, Role badge, Primary Department, Status (Active/Inactive), Availability, Last Login
- [ ] Filter: role, department, active/inactive, search
- [ ] "New User" button → form dialog (fullName, email, temp password, role, primaryDepartment)
- [ ] Row click → `/admin/users/{id}` — detail with department assignments, skill tags, deactivate/reactivate button
- [ ] Department assignment: multi-select with primary flag radio button
- [ ] Skill assignment: tag-style multi-select of categories

## Technical Notes
- Components: `UserListComponent`, `UserDetailComponent`, `UserFormComponent`
- Services: `UserService.list()`, `UserService.create()`, `UserService.update()`, `UserService.deactivate()`, `UserService.updateDepartments()`, `UserService.updateSkills()`
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-063, US-BE-064, US-BE-065, US-BE-066, US-FE-026
