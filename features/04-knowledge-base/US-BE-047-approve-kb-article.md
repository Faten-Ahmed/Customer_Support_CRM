# US-BE-047 — Approve KB Article

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
**As a** manager, **I want to** approve a submitted article, **so that** it becomes visible to agents and customers.

## Acceptance Criteria
- [ ] `POST /kb/articles/{id}/approve` transitions `Status: PendingReview → Published`; sets `PublishedAt = now`
- [ ] Agent role returns `403`
- [ ] Article in any state other than `PendingReview` returns `422` with code `INVALID_STATUS_TRANSITION`
- [ ] `KbArticlePublished` event published → author notified

## Technical Notes
- Endpoint: `POST /kb/articles/{id}/approve`
- Entity: `KbArticle`
- Business rule: BR-KB-001
- Spec: `specs/api/knowledge-base.md`

## Dependencies
- US-BE-046, US-BE-054
