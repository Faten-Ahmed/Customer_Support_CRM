# US-FE-041 — Agent AI Assistant Panel

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
**As an** agent, **I want** an AI assistant I can chat with to get help with policies, drafts, or information, **so that** I can resolve tickets faster.

## Acceptance Criteria
- [ ] Slide-in panel accessible from any page (icon in sidebar or header)
- [ ] Chat interface: message list + input + send button
- [ ] Uses `context = "agent"` — no handoff logic
- [ ] Session persists within current browser session; cleared on logout
- [ ] Shows `suggestedArticles` as clickable cards when AI recommends KB content
- [ ] "New Chat" button resets session

## Technical Notes
- Component: `AgentAiAssistantComponent`
- Service: `AiChatService.sendMessage({ context: "agent" })`
- Spec: `specs/api/ai.md`

## Dependencies
- US-BE-087, US-FE-005
