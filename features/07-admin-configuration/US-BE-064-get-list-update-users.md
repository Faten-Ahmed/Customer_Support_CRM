# US-BE-064 — Get, List, and Update Users

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
**As an** admin, **I want to** view and update user profiles, **so that** I can keep staff records accurate.

## Acceptance Criteria
- [ ] `GET /admin/users` returns paginated list; filter by `role`, `departmentId`, `isActive`, `search`
- [ ] `GET /admin/users/{id}` returns full profile including `firstName`, `lastName`, `firstNameAr`, `lastNameAr`, `jobTitle`, `jobTitleAr`, `departments[]`, `skills[]`, `availabilityStatus`
- [ ] `PUT /admin/users/{id}` updates `firstName`, `lastName`, `firstNameAr`, `lastNameAr`, `jobTitle`, `jobTitleAr`, `primaryDepartmentId`; returns updated object
- [ ] Role cannot be changed via `PUT` — field is ignored if supplied (BR-ADM-003)
- [ ] Response does NOT include `PasswordHash`

## Technical Notes
- Endpoints: `GET /admin/users`, `GET /admin/users/{id}`, `PUT /admin/users/{id}`
- Entity: `User`, `AgentDepartment`, `AgentSkill`
- Business rule: BR-ADM-003
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-063
