# US-BE-045 — Create KB Article (Draft)

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
**As an** agent, **I want to** create a knowledge base article draft, **so that** I can write and refine it before it goes through review.

## Acceptance Criteria
- [ ] `POST /kb/articles` with `title`, `categoryId` (required) creates article with `Status = Draft`; returns `201`
- [ ] `content`, `titleAr`, `contentAr` are optional at creation (can be added before submit)
- [ ] `categoryId` must reference an active `KbCategory`; invalid returns `422`
- [ ] `visibility` defaults to `Internal` if not provided
- [ ] Returned object includes `id`, `status`, `createdBy`, `createdAt`

## Technical Notes
- Endpoint: `POST /kb/articles`
- Entity: `KbArticle`
- Business rule: BR-KB-011
- Spec: `specs/api/knowledge-base.md`, `specs/features/04-knowledge-base.md`

## Dependencies
- US-BE-007
