# US-FE-039 — Portal Live Chat UI (After Handoff)

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
**As a** customer transferred to a live agent, **I want** the chatbot to seamlessly switch to a live conversation, **so that** the transition is smooth.

## Acceptance Criteria
- [ ] On `handoffRequired = true`: widget title changes to "Connecting to support agent..."
- [ ] Client calls ChatHub `JoinSession + RequestHandoff`
- [ ] `HandoffAccepted` event received → widget title: "Connected with {agentName}"
- [ ] Messages continue in same thread (ChatSession history preserved)
- [ ] Typing indicators shown for agent
- [ ] If no agent accepts within 3 minutes: "No agents are available right now. We'll follow up via ticket." and ticket is created
- [ ] Session close by agent: shows "Chat ended by agent" and link to ticket

## Technical Notes
- Component: `LiveChatComponent` (extended from chatbot widget)
- SignalR: `ChatHubService` — `JoinSession`, `RequestHandoff`, `SendMessage`, subscribes to `ReceiveMessage`, `HandoffAccepted`, `SessionClosed`
- Spec: `specs/api/communication.md`

## Dependencies
- US-BE-091, US-FE-038
