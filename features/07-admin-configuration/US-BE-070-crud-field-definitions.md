# US-BE-070 — CRUD Ticket Field Definitions

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
**As an** admin, **I want to** define custom fields per department, **so that** agents capture department-specific data on tickets.

## Acceptance Criteria
- [ ] `GET /admin/field-definitions` lists all; filter by `departmentId`, `categoryId`
- [ ] `POST /admin/field-definitions` with `departmentId`, `fieldName`, `fieldType` (required); `options` array required for `Dropdown` type (2–20 items)
- [ ] `PUT /admin/field-definitions/{id}` updates allowed fields; returns `200`
- [ ] `DELETE /admin/field-definitions/{id}` soft-deactivates (`isActive = false`); existing values retained; returns `204`
- [ ] Dropdown with < 2 options returns `422`

## Technical Notes
- Endpoints: CRUD on `/admin/field-definitions`
- Entity: `TicketFieldDefinition`
- Business rules: BR-ADM-020—023
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-067, US-BE-069, US-BE-007
