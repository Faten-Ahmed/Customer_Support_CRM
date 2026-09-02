# US-BE-083 — AI Ticket Summarization

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
**As an** agent, **I want to** generate a one-click summary of a ticket's thread, **so that** I can quickly understand a long conversation.

## Acceptance Criteria
- [ ] `POST /ai/tickets/{id}/summarize` returns a 2–4 sentence summary of the ticket's full message thread
- [ ] Ticket with zero messages returns `422` with code `NO_MESSAGES_TO_PROCESS`
- [ ] Includes all messages (including internal notes) in the prompt context
- [ ] Summary NOT persisted; returned in response only
- [ ] `modelUsed` field in response (Azure deployment name)
- [ ] Azure OpenAI 30s timeout → `503` with code `AI_PROVIDER_UNAVAILABLE`
- [ ] Azure endpoint validated at startup to be UAE/Europe region

## Technical Notes
- Endpoint: `POST /ai/tickets/{id}/summarize`
- Azure OpenAI: GPT-4o-mini deployment
- Business rules: BR-AI-001—007
- Spec: `specs/api/ai.md`, `specs/features/10-ai-features.md`

## Dependencies
- US-BE-028, US-BE-007
