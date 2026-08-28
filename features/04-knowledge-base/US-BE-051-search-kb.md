# US-BE-051 — Search KB Articles

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
**Roles:** Admin, Manager, Agent, Customer
**As an** agent or customer, **I want to** search the knowledge base by keyword, **so that** I can find relevant articles without browsing categories.

## Acceptance Criteria
- [ ] `GET /kb/search?q=` performs SQL Server full-text search on `Title`, `TitleAr`, `Content`, `ContentAr`
- [ ] Minimum query length: 2 characters; shorter returns `422` with code `QUERY_TOO_SHORT`
- [ ] Results ranked by FTS relevance score, tiebroken by `PublishedAt DESC`
- [ ] Agent endpoint: searches all `Published` articles regardless of visibility
- [ ] `GET /portal/kb/search?q=` scoped to `Published + Public/Both` only
- [ ] Returns `excerpt` (first 200 chars of matched content)

## Technical Notes
- Endpoints: `GET /kb/search`, `GET /portal/kb/search`
- Entity: `KbArticle` (FTS index on Title, TitleAr, Content, ContentAr)
- Business rules: BR-KB-007, BR-KB-008, BR-KB-009, BR-KB-010
- Spec: `specs/api/knowledge-base.md`, `specs/api/customer-portal.md`

## Dependencies
- US-BE-047
