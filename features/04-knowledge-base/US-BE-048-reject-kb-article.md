# US-BE-048 — Reject KB Article

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
**As a** manager, **I want to** reject a submitted article with a note, **so that** the author knows what to fix.

## Acceptance Criteria
- [ ] `POST /kb/articles/{id}/reject` with `{ "rejectionNote": "..." }` transitions `Status: PendingReview → Draft`
- [ ] `rejectionNote` required (min 10 chars); missing returns `422`
- [ ] Agent role returns `403`
- [ ] `KbArticleRejected` event published → author notified with rejection note

## Technical Notes
- Endpoint: `POST /kb/articles/{id}/reject`
- Entity: `KbArticle`
- Business rule: BR-KB-003
- Spec: `specs/api/knowledge-base.md`

## Dependencies
- US-BE-046, US-BE-054
