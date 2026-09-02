# US-BE-086 — AI Suggest Articles

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


**Epic:** AI Features
**Roles:** Admin, Manager, Agent
**As an** agent, **I want to** get AI-recommended KB articles for a ticket, **so that** I can quickly find self-service resources for the customer.

## Acceptance Criteria
- [ ] `POST /ai/tickets/{id}/suggest-articles` returns up to 5 articles with `articleId`, `title`, `titleAr`, `relevanceScore`, `excerpt` (first 200 chars)
- [ ] Only `Published + Public/Both` articles are candidates
- [ ] If > 1000 published articles exist: pre-filter top 50 via SQL Server FTS before sending to AI
- [ ] Empty KB → returns `suggestions: []` without calling AI
- [ ] Agent can use results to share article link; no auto-send

## Technical Notes
- Endpoint: `POST /ai/tickets/{id}/suggest-articles`
- Business rules: BR-AI-015—017
- Spec: `specs/api/ai.md`

## Dependencies
- US-BE-083, US-BE-047
