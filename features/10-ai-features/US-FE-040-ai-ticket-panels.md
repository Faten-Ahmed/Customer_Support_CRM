# US-FE-040 — AI Panels in Ticket Detail

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
**As an** agent, **I want to** use AI assistance directly from the ticket detail page, **so that** I can work faster without switching tools.

## Acceptance Criteria
- [ ] Collapsible right panel in ticket detail with 4 AI tools:
  1. **Summarize**: "Summarize Ticket" button → shows 2–4 sentence summary in a card; "Copy" button
  2. **Suggest Reply**: tone selector (Professional/Friendly/Formal), language toggle (EN/AR) → "Generate Reply" → opens reply in composer (pre-fills textarea)
  3. **Suggest Category**: shows up to 3 suggestions with confidence band badges; "Apply" button per suggestion → calls `PUT /tickets/{id}` to set category
  4. **Suggest Articles**: shows up to 5 article cards with title, excerpt; "Share with customer" button adds article link to composer
- [ ] Loading spinner per tool while AI is thinking
- [ ] On `503 AI_PROVIDER_UNAVAILABLE`: shows "AI service is temporarily unavailable"
- [ ] Each tool independent — can use any subset

## Technical Notes
- Component: `AiTicketPanelComponent`
- Services: `AiService.summarize()`, `AiService.suggestReply()`, `AiService.suggestCategory()`, `AiService.suggestArticles()`
- Spec: `specs/api/ai.md`

## Dependencies
- US-BE-083, US-BE-084, US-BE-085, US-BE-086, US-FE-010
