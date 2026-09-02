# US-BE-049 — Archive KB Article

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
**Roles:** Admin, Manager
**As a** manager, **I want to** archive an outdated article, **so that** it stops appearing in search results without losing the content.

## Acceptance Criteria
- [ ] `POST /kb/articles/{id}/archive` transitions `Status → Archived`
- [ ] Any status can be archived (Draft, PendingReview, Published); returns `200`
- [ ] Agent role returns `403`
- [ ] Archived article no longer appears in `GET /kb/articles`, `GET /portal/kb/articles`, or AI suggest-articles results
- [ ] `KbArticleArchived` event published

## Technical Notes
- Endpoint: `POST /kb/articles/{id}/archive`
- Entity: `KbArticle`
- Business rule: BR-KB-015
- Spec: `specs/api/knowledge-base.md`, `specs/features/04-knowledge-base.md` W-KB-02

## Dependencies
- US-BE-045, US-BE-007
