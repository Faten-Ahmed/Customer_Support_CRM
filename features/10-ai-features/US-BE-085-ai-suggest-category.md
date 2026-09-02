# US-BE-085 — AI Suggest Category

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
**As an** agent, **I want to** get AI-suggested categories for a ticket, **so that** I classify tickets accurately without guessing.

## Acceptance Criteria
- [ ] `POST /ai/tickets/{id}/suggest-category` returns up to 3 suggestions with `categoryId`, `categoryName`, `parentCategoryName`, `confidence`, `confidenceBand`, `label`
- [ ] Confidence bands: High ≥ 0.80, Medium 0.50–0.80, Low < 0.50
- [ ] Suggestions with confidence < 0.20 filtered out
- [ ] AI-hallucinated category IDs not in the active list silently filtered
- [ ] Agent must manually confirm via `PUT /tickets/{id}` — no auto-apply

## Technical Notes
- Endpoint: `POST /ai/tickets/{id}/suggest-category`
- Business rules: BR-AI-011—014
- Spec: `specs/api/ai.md`

## Dependencies
- US-BE-083, US-BE-069
