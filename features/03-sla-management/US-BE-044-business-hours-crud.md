# US-BE-044 — Business Hours & Holiday CRUD

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


**Epic:** SLA Management
**Roles:** Admin
**As an** admin, **I want to** define working days, hours, and public holidays, **so that** SLA clocks only count business time.

## Acceptance Criteria
- [ ] `GET /admin/business-hours` returns global and all department-specific records including `holidays[]`
- [ ] `PUT /admin/business-hours/{id}` updates `workDays`, `startTime`, `endTime`, `timeZone`
- [ ] `workDays` must contain 1–7 valid day names; empty array returns `422`
- [ ] `timeZone` must be a valid IANA string; invalid returns `422` with code `INVALID_TIMEZONE`
- [ ] `startTime < endTime` required; crossing midnight not supported (returns `422`)
- [ ] `POST /admin/business-hours/{id}/holidays` adds a holiday; duplicate date on same record returns `409`
- [ ] `DELETE /admin/business-hours/{id}/holidays/{holidayId}` removes a holiday; returns `204`

## Technical Notes
- Endpoints: `GET/PUT /admin/business-hours/{id}`, `POST/DELETE /admin/business-hours/{id}/holidays/{holidayId}`
- Entity: `BusinessHours`, `Holiday`
- Business rules: BR-ADM-031—036
- Spec: `specs/api/admin.md`, `specs/features/07-admin-configuration.md`

## Dependencies
- US-BE-007
