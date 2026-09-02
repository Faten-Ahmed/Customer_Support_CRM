# US-BE-046 — Submit KB Article for Review

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
**Roles:** Admin, Manager, Agent (own articles)
**As an** agent, **I want to** submit my draft article for manager review, **so that** it can be approved and published.

## Acceptance Criteria
- [ ] `POST /kb/articles/{id}/submit-review` transitions `Status: Draft → PendingReview`
- [ ] `content` must be ≥ 100 characters; too short returns `422`
- [ ] If the department requires Arabic: `titleAr` and `contentAr` must be present (BR-KB-006); missing returns `422`
- [ ] Only the article author or Manager+ can submit; other agents return `403`
- [ ] `KbArticleSubmittedForReview` event published → notification sent to Manager/Admin

## Technical Notes
- Endpoint: `POST /kb/articles/{id}/submit-review`
- Entity: `KbArticle`
- Business rules: BR-KB-002, BR-KB-006
- Spec: `specs/api/knowledge-base.md`, `specs/features/04-knowledge-base.md` W-KB-01

## Dependencies
- US-BE-045, US-BE-054
