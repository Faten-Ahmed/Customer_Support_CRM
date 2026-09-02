# US-FE-020 — Quick-Reply Template Management Panel

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
**As an** agent, **I want to** manage my personal reply templates, **so that** I can keep my quick-reply library organised.

## Acceptance Criteria
- [ ] Route: `/settings/templates`
- [ ] Two sections: "My Templates" (Personal) and "Global Templates" (read-only for agents)
- [ ] Create button: opens form dialog with title, content (textarea), category
- [ ] Edit/Delete actions on own templates; hover shows action icons
- [ ] Search/filter by title or category
- [ ] Template preview shown on hover or click

## Technical Notes
- Component: `TemplateManagementComponent`
- Service: `TemplateService.list()`, `TemplateService.create()`, `TemplateService.update()`, `TemplateService.delete()`
- Spec: `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-060, US-FE-005
