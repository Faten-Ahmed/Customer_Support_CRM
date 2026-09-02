# US-FE-035 — Portal Knowledge Base

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


**Epic:** Customer Portal
**Roles:** Customer
**As a** customer, **I want to** search and read help articles, **so that** I can solve common issues without opening a ticket.

## Acceptance Criteria
- [ ] Route: `/portal/kb` — search bar prominently at top; category grid below; recent/featured articles list
- [ ] Search results page (`/portal/kb?q=`): article cards with title, excerpt, category; ranked by relevance
- [ ] Category browse: click category → filtered article list
- [ ] Route: `/portal/kb/{id}` — full article with Markdown rendered; bilingual (language toggle shows AR if `contentAr` exists)
- [ ] "Was this article helpful?" thumbs up/down (UI only in v1 — no backend)
- [ ] "Still need help?" CTA linking to `/portal/tickets/new`

## Technical Notes
- Components: `PortalKbHomeComponent`, `PortalKbSearchComponent`, `PortalKbArticleComponent`
- Services: `PortalKbService.list()`, `PortalKbService.search()`, `PortalKbService.getById()`
- Spec: `specs/api/customer-portal.md`

## Dependencies
- US-BE-050, US-BE-051, US-FE-032
