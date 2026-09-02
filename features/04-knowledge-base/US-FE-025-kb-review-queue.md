# US-FE-025 — KB Review Queue (Manager)

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
**Roles:** Manager, Admin
**As a** manager, **I want to** review and approve or reject submitted articles, **so that** only quality content is published.

## Acceptance Criteria
- [ ] Route: `/kb?status=PendingReview` — or a dedicated "Review Queue" tab
- [ ] Article detail view at `/kb/articles/{id}` shows Approve and Reject buttons (Manager+ only)
- [ ] Approve: one-click confirmation dialog → article published
- [ ] Reject: dialog with required rejection note textarea (min 10 chars) → article returned to Draft
- [ ] After approve/reject: navigates back to list; success toast

## Technical Notes
- Component: `KbArticleDetailComponent` (add approve/reject actions)
- Services: `KbService.approve()`, `KbService.reject()`
- Spec: `specs/api/knowledge-base.md`

## Dependencies
- US-BE-047, US-BE-048, US-FE-024
