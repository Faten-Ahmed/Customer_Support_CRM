# US-BE-060 — CRUD Personal Quick-Reply Templates

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


**Epic:** Agent Dashboard
**Roles:** Agent, Manager, Admin
**As an** agent, **I want to** create, edit, and delete my own quick-reply templates, **so that** I can respond faster to common ticket types.

## Acceptance Criteria
- [ ] `POST /agents/me/templates` with `title`, `content` (required), optional `category` creates a `Personal` scope template; returns `201`
- [ ] `GET /agents/me/templates` returns both `Personal` (owned by caller) and `Global` templates
- [ ] `PUT /agents/me/templates/{id}` updates own `Personal` template; returns `200`
- [ ] `DELETE /agents/me/templates/{id}` deletes own `Personal` template; returns `204`
- [ ] Attempting to edit or delete a `Global` template returns `403`
- [ ] `title` max 100 chars; `content` max 2000 chars

## Technical Notes
- Endpoints: CRUD on `/agents/me/templates`
- Entity: `QuickReplyTemplate` (`scope`, `ownerId`)
- Business rules: BR-AGT-012, BR-AGT-013
- Spec: `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-007
