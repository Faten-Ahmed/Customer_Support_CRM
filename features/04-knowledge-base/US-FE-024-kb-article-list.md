# US-FE-024 — KB Article List & Editor (Agent)

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


**Epic:** Knowledge Base
**Roles:** Admin, Manager, Agent
**As an** agent, **I want to** browse KB articles and write new ones, **so that** I can share knowledge and contribute to the team's resources.

## Acceptance Criteria
- [ ] Route: `/kb` — article list with columns: Title, Category, Status badge, Visibility badge, Author, Published date
- [ ] Filter: by status, category, visibility; search by title (debounced)
- [ ] "New Article" button → `/kb/articles/new` — Markdown editor (Monaco or ngx-markdown-editor) with live preview
- [ ] Editor form: title (EN + AR), content (EN + AR), category selector, visibility selector
- [ ] "Save Draft" and "Submit for Review" buttons
- [ ] Edit existing draft → same editor at `/kb/articles/{id}/edit`
- [ ] Status badge colour-coded (Draft=grey, PendingReview=orange, Published=green, Archived=dark)

## Technical Notes
- Components: `KbArticleListComponent`, `KbArticleEditorComponent`
- Services: `KbService.list()`, `KbService.create()`, `KbService.update()`, `KbService.submitForReview()`
- Spec: `specs/api/knowledge-base.md`

## Dependencies
- US-BE-045, US-BE-046, US-FE-005
