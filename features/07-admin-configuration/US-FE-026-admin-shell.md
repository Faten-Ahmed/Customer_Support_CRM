# US-FE-026 — Admin Navigation Shell

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
**Roles:** Admin, Manager
**As an** admin, **I want** a dedicated admin section with a clear navigation menu, **so that** configuration tasks are separate from day-to-day ticket work.

## Acceptance Criteria
- [ ] Route prefix: `/admin` — guarded by `RoleGuard` for Admin/Manager roles
- [ ] Left nav: Users, Departments, Branches, Categories, Field Definitions, SLA Policies, Business Hours, Templates, Channel Status
- [ ] Manager sees: Departments, Categories, SLA Policies, Business Hours (read-only)
- [ ] Active nav item highlighted
- [ ] Breadcrumb trail for nested pages
- [ ] Responsive layout (collapses to icon-only on narrow screens)

## Technical Notes
- Component: `AdminShellComponent` with `RouterOutlet`
- Route guards: `RoleGuard(['Admin', 'Manager'])`
- Lazy-loaded `AdminModule`

## Dependencies
- US-FE-005
