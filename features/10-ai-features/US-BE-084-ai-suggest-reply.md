# US-BE-084 — AI Suggest Reply

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
**As an** agent, **I want to** generate a suggested reply draft with a chosen tone and language, **so that** I can reply faster without starting from scratch.

## Acceptance Criteria
- [ ] `POST /ai/tickets/{id}/suggest-reply` with `tone` (professional/friendly/formal) and `language` (en/ar) returns `suggestedReply` text
- [ ] Prompt uses last customer message as primary target; last 5 messages as context
- [ ] `language = "ar"` uses `gpt-4o` deployment (configurable); response is in Arabic
- [ ] Reply is NOT sent automatically; agent must manually copy/edit and use `POST /tickets/{id}/messages`
- [ ] Ticket with zero messages returns `422` with code `NO_MESSAGES_TO_PROCESS`
- [ ] Invalid `tone` or `language` returns `422`

## Technical Notes
- Endpoint: `POST /ai/tickets/{id}/suggest-reply`
- Business rules: BR-AI-008—010
- Spec: `specs/api/ai.md`

## Dependencies
- US-BE-083
