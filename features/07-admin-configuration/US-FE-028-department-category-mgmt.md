# US-FE-028 — Department, Branch & Category Management

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
**Roles:** Admin (write), Manager (read)
**As an** admin, **I want to** manage departments, branches, and the category tree, **so that** the org structure is always up-to-date.

## Acceptance Criteria
- [ ] Route: `/admin/departments` — table with name, nameAr, agent count, business hours, status; create/edit/deactivate actions
- [ ] Route: `/admin/branches` — simpler table; create/edit/deactivate
- [ ] Route: `/admin/categories` — tree view: expandable parent rows with child rows indented; add child button per parent; deactivate propagates to children (confirm dialog warns)
- [ ] All create/edit via dialog forms
- [ ] Deactivate confirms with count of open tickets if any; blocks if tickets exist
- [ ] Drag-handle to reorder `sortOrder` (optional enhancement — flag for v2 if complex)

## Technical Notes
- Components: `DepartmentListComponent`, `BranchListComponent`, `CategoryTreeComponent`
- Services: `DepartmentService`, `BranchService`, `CategoryService`
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-067, US-BE-068, US-BE-069, US-FE-026
