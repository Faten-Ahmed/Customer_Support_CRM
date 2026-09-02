# US-BE-067 — CRUD Departments

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
**Roles:** Admin (write), Admin + Manager (read)
**As an** admin, **I want to** create and manage departments, **so that** tickets can be routed to the right team.

## Acceptance Criteria
- [ ] `POST /admin/departments` with `name` (unique, required), optional `nameAr`, `description`, `businessHoursId`; returns `201`
- [ ] `GET /admin/departments` returns all departments with `agentCount`, `businessHours` summary, `isActive`
- [ ] `PUT /admin/departments/{id}` updates name, description, businessHoursId; returns `200`
- [ ] `POST /admin/departments/{id}/deactivate` fails if department has open tickets; returns `422` with count (BR-ADM-011)
- [ ] `POST /admin/departments/{id}/reactivate` re-enables routing; returns `200`
- [ ] Duplicate department name returns `409`

## Technical Notes
- Endpoints: CRUD on `/admin/departments`, `/admin/departments/{id}/deactivate|reactivate`
- Entity: `Department`
- Business rules: BR-ADM-010—013
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-007
