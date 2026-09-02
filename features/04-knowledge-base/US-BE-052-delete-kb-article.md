# US-BE-052 — Delete KB Article (Draft Only)

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
**Roles:** Admin, Manager, Agent (own draft)
**As an** agent, **I want to** delete a draft article I created by mistake, **so that** it doesn't clutter the article list.

## Acceptance Criteria
- [ ] `DELETE /kb/articles/{id}` hard-deletes `Draft` articles; returns `204`
- [ ] Attempting to delete a `Published` or `Archived` article returns `422` with code `MUST_ARCHIVE_FIRST`
- [ ] Only the article author can delete their own draft; Manager+ can delete any draft
- [ ] Other agents' drafts: `403`

## Technical Notes
- Endpoint: `DELETE /kb/articles/{id}`
- Entity: `KbArticle`
- Business rule: BR-KB-014
- Spec: `specs/api/knowledge-base.md`, `specs/features/04-knowledge-base.md`

## Dependencies
- US-BE-045, US-BE-007
