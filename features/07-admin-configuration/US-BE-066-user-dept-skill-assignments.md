# US-BE-066 — User Department & Skill Assignments

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
**As an** admin, **I want to** assign agents to departments and skill categories, **so that** the auto-assignment engine can route tickets correctly.

## Acceptance Criteria
- [ ] `PUT /admin/users/{id}/departments` replaces full department list; exactly one must have `isPrimary = true`; violation returns `422` with code `MULTIPLE_PRIMARY_DEPARTMENTS` or `NO_PRIMARY_DEPARTMENT`
- [ ] Must include at least 1 department; empty array returns `422`
- [ ] `PUT /admin/users/{id}/skills` replaces full skill (category) list atomically; empty array clears all skills
- [ ] Unknown `categoryIds` return `422`
- [ ] Both endpoints return the updated user object with new assignments

## Technical Notes
- Endpoints: `PUT /admin/users/{id}/departments`, `PUT /admin/users/{id}/skills`
- Entities: `AgentDepartment`, `AgentSkill`
- Business rules: BR-ADM-007, BR-ADM-008
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-063, US-BE-072
