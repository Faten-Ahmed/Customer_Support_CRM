# US-BE-087 — AI Chat Message (Portal Chatbot & Agent Assistant)

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
**Roles:** Any authenticated user
**As a** customer, **I want to** chat with an AI bot on the portal, **so that** I can get instant help without waiting for an agent.

## Acceptance Criteria
- [ ] `POST /ai/chat/message` with `sessionId`, `message`, `context` (portal|agent) returns `sessionId`, `reply`, `suggestedArticles[]`, `handoffRequired`, `handoffReason`
- [ ] `sessionId = null` creates a new `ChatSession`; returned `sessionId` used for all subsequent messages
- [ ] `handoffRequired = true` triggers when: 3 failed understanding attempts, customer says "human/agent/real person", session > 10 minutes old and unresolved, sensitive topic detected
- [ ] `context = "agent"`: no handoff logic; AI acts as internal assistant
- [ ] Concurrent messages on same session: second request returns `409` with code `SESSION_BUSY`
- [ ] Timeout 30s → `503` with code `AI_PROVIDER_UNAVAILABLE`
- [ ] Session history (last 20 messages) sent as context on each call

## Technical Notes
- Endpoint: `POST /ai/chat/message`
- Entity: `ChatSession`, `ChatSessionMessage`
- Business rules: BR-AI-018—026
- Spec: `specs/api/ai.md`, `specs/features/10-ai-features.md`

## Dependencies
- US-BE-007, US-BE-086
