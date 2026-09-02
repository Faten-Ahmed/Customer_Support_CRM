# US-FE-021 — Personal Tasks Panel

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
**As an** agent, **I want to** manage a personal task list in the CRM, **so that** I track follow-up actions without switching tools.

## Acceptance Criteria
- [ ] Route: `/tasks` or slide-in panel accessible from dashboard
- [ ] Task list: incomplete tasks first (sorted by due date), then completed
- [ ] Add task: inline form with title (required), description, due date picker
- [ ] Check box to mark complete; completed tasks shown with strikethrough
- [ ] Delete task: trash icon with immediate delete (no confirmation needed)
- [ ] Past-due tasks highlighted in red
- [ ] Counter badge showing incomplete task count

## Technical Notes
- Component: `PersonalTasksComponent`
- Service: `TaskService.list()`, `TaskService.create()`, `TaskService.update()`, `TaskService.delete()`
- Spec: `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-062, US-FE-005
