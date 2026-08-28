# US-BE-071 — Admin Global Template CRUD

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
**As an** admin, **I want to** create global quick-reply templates visible to all agents, **so that** the organisation has consistent messaging standards.

## Acceptance Criteria
- [ ] `POST /admin/templates` with `title`, `content` (required), optional `category` creates `scope = Global` template; returns `201`
- [ ] `GET /admin/templates` lists only `Global` scope templates
- [ ] `PUT /admin/templates/{id}` and `DELETE /admin/templates/{id}` scoped to Global templates only
- [ ] Agents cannot create, edit, or delete Global templates — `403`

## Technical Notes
- Endpoints: CRUD on `/admin/templates`
- Entity: `QuickReplyTemplate` (`scope = Global`)
- Business rules: BR-ADM-024, BR-ADM-025
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-060, US-BE-007
