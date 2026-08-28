# US-BE-063 — Create Internal User (Admin)

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
**As an** admin, **I want to** create accounts for new agents and managers, **so that** they can access the CRM.

## Acceptance Criteria
- [ ] `POST /admin/users` with `fullName`, `email`, `password`, `role`, `primaryDepartmentId` (required for Agent/Manager) creates user; returns `201`
- [ ] Duplicate email returns `409`
- [ ] `primaryDepartmentId` required for Agent/Manager roles; missing returns `422`
- [ ] Created with `PasswordMustChange = true`, `IsActive = true`
- [ ] `SendWelcomeEmailJob` enqueued with login URL and temp password notification

## Technical Notes
- Endpoint: `POST /admin/users`
- Entity: `User`, `AgentDepartment`
- Business rules: BR-ADM-001—003, BR-ADM-009
- Spec: `specs/api/admin.md`, `specs/features/07-admin-configuration.md` W-ADM-01

## Dependencies
- US-BE-007, US-BE-008
