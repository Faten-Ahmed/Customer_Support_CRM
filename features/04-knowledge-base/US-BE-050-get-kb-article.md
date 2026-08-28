# US-BE-050 — Get & List KB Articles

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
**Roles:** Admin, Manager, Agent (internal); Customer (portal, filtered)
**As an** agent, **I want to** view individual articles and browse the full list, **so that** I can find reference material quickly.

## Acceptance Criteria
- [ ] `GET /kb/articles/{id}` (agent+) returns full article regardless of status (Draft/PendingReview/Published/Archived)
- [ ] `GET /kb/articles` lists articles; filter by `status`, `categoryId`, `visibility`; paginated
- [ ] `GET /portal/kb/articles/{id}` returns `403` if `Visibility = Internal` or `Status != Published`
- [ ] `GET /portal/kb/articles` returns only `Published + Public/Both` articles; paginated
- [ ] Article response includes `title`, `titleAr`, `content`, `contentAr`, `visibility`, `status`, `publishedAt`

## Technical Notes
- Endpoints: `GET /kb/articles`, `GET /kb/articles/{id}`, `GET /portal/kb/articles`, `GET /portal/kb/articles/{id}`
- Entity: `KbArticle`
- Business rules: BR-KB-004, BR-KB-005
- Spec: `specs/api/knowledge-base.md`, `specs/api/customer-portal.md`

## Dependencies
- US-BE-045, US-BE-007
