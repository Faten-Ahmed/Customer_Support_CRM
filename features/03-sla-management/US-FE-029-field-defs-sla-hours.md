# US-FE-029 — Field Definitions, SLA Policies & Business Hours

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
**As an** admin, **I want to** configure custom fields, SLA targets, and working hours, **so that** tickets and SLA calculations work correctly.

## Acceptance Criteria
- [ ] Route: `/admin/field-definitions` — list grouped by department; create/edit/deactivate; type selector with dynamic options field (shown only for Dropdown type)
- [ ] Route: `/admin/sla` — table of policies grouped by priority; inline-edit for time values; validation errors shown inline
- [ ] Route: `/admin/business-hours` — card per department + global; edit work days (checkbox grid), start/end time pickers, timezone selector; holiday list with add/remove per card
- [ ] All changes show unsaved state indicator; save button per card

## Technical Notes
- Components: `FieldDefinitionListComponent`, `SlaPolicyTableComponent`, `BusinessHoursEditorComponent`
- Services: `FieldDefinitionService`, `SlaPolicyService`, `BusinessHoursService`
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-070, US-BE-043, US-BE-044, US-FE-026
