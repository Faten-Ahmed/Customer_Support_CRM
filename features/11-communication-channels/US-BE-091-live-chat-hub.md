# US-BE-091 — Live Chat (SignalR ChatHub)

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


**Epic:** Communication Channels
**Roles:** Customer, Agent
**As a** customer, **I want to** be connected to a live agent via chat after the bot cannot help, **so that** I get real human support.

## Acceptance Criteria
- [ ] `ChatHub` authenticates via JWT query param; customer and agent connections routed to `chat-{sessionId}` group
- [ ] `JoinSession`: validates caller owns or is assigned to the session; adds to group
- [ ] `RequestHandoff` (customer): creates Ticket from ChatSession transcript (`Channel = LiveChat`); broadcasts `HandoffRequested` to dept agent group
- [ ] `AgentAcceptHandoff`: sets `ChatSession.AgentId`, `Status = AgentConnected`; broadcasts `HandoffAccepted` to session group
- [ ] `SendMessage`: persists `ChatSessionMessage` + `TicketMessage` (dual-write); broadcasts `ReceiveMessage` to session group
- [ ] `CloseSession` with `resolution = Resolved` → linked Ticket becomes Resolved; `Escalated` → ticket escalated
- [ ] `AgentTyping` / `CustomerTyping`: fire-and-forget broadcast; not persisted
- [ ] `SubscribeToDepartment`: adds agent connection to `dept-chat-{departmentId}` group

## Technical Notes
- Hub URL: `ws://localhost:5000/hubs/chat`
- Entity: `ChatSession`, `ChatSessionMessage`, `Ticket`, `TicketMessage`
- Business rules: BR-COM-023—028, BR-AI-018—020
- Spec: `specs/api/communication.md`

## Dependencies
- US-BE-087, US-BE-019, US-BE-028
