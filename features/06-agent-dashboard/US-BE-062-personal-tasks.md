# US-BE-062 — CRUD Personal Tasks

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
**As an** agent, **I want to** manage a personal to-do list, **so that** I track follow-up actions tied to my support work.

## Acceptance Criteria
- [ ] `POST /agents/me/tasks` with `title` (required), optional `description`, `dueDate`; returns `201`
- [ ] `GET /agents/me/tasks` returns caller's tasks: incomplete first, then by `dueDate ASC`, then `createdAt ASC`
- [ ] `PUT /agents/me/tasks/{id}` updates `title`, `description`, `dueDate`, `isCompleted`
- [ ] `DELETE /agents/me/tasks/{id}` hard-deletes; returns `204`
- [ ] Max 200 incomplete tasks per agent; exceeding `POST` returns `422` with code `MAX_TASKS_REACHED`
- [ ] Completed tasks purged after 30 days by `PurgeCompletedTasksJob` (nightly Hangfire)

## Technical Notes
- Endpoints: CRUD on `/agents/me/tasks`
- Entity: `AgentTask`
- Business rules: BR-AGT-016—019
- Spec: `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-007
