# US-FE-038 — Portal Chatbot Widget

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
**As a** customer, **I want to** chat with an AI assistant from any portal page, **so that** I can get instant help without navigating away.

## Acceptance Criteria
- [ ] Floating chat bubble (bottom-right) visible on all portal pages
- [ ] Click opens chat window: message list + text input + send button
- [ ] First message: creates new session (`sessionId = null`); subsequent messages reuse session
- [ ] AI replies appear with typing indicator while loading
- [ ] `suggestedArticles` shown as clickable cards below the AI reply
- [ ] When `handoffRequired = true`: shows "Connecting you to an agent..." then transitions to live chat mode (US-FE-039)
- [ ] Close button minimises widget; session preserved for current page visit

## Technical Notes
- Component: `ChatbotWidgetComponent` (portal layout shell includes it)
- Service: `AiChatService.sendMessage()`
- Spec: `specs/api/ai.md`, `specs/features/10-ai-features.md`

## Dependencies
- US-BE-087, US-FE-032
