# Feature Spec — AI Features

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


> Requirements: `REQ-AI-*`
> API: `specs/api/ai.md`
> Domain entities: `Ticket`, `TicketMessage`, `KbArticle`, `TicketCategory`, `ChatSession`, `ChatSessionMessage`

---

## Overview

AI features are advisory only — they suggest, never auto-apply. All AI processing uses Azure OpenAI (UAE/Europe region mandatory for data residency compliance). US-region endpoints are prohibited. The AI subsystem is a thin integration layer: it assembles context from the domain, calls the AI provider, and returns structured suggestions for human review.

---

## Mandatory Constraints

**BR-AI-001** All Azure OpenAI API calls must target the UAE or Europe region endpoint. Configuring a US-region endpoint causes the application to fail startup validation (`IStartupFilter` checks `AzureOpenAI:Endpoint` against an allowed-regions allowlist).

**BR-AI-002** No ticket content, customer PII, or message text is sent to any AI provider other than Azure OpenAI. Any future AI provider addition requires explicit security review and data residency verification.

**BR-AI-003** AI features are advisory: no AI-generated content is automatically applied to a ticket. Summarize, suggest-reply, suggest-category, and suggest-articles all return suggestions that an agent must manually act on.

**BR-AI-004** AI features are only available for tickets with at least one `TicketMessage` record (BR-AI-004 enforced at the controller level — `422` with code `NO_MESSAGES_TO_PROCESS` if ticket has zero messages).

---

## POST /ai/tickets/{id}/summarize

### Input Assembly
The handler fetches:
- Ticket: `Subject`, `Description`, `Status`, `Priority`, `Category`
- All `TicketMessage` records for the ticket, sorted by `CreatedAt ASC`, with `SenderName`, `SenderType` (Agent/Customer), `IsInternal` flag, `Content`
- Internal notes ARE included in the summary context (agents use this; customers never call this endpoint)

### Prompt Design
```
System: You are a support ticket summarizer. Produce a 2–4 sentence summary 
        covering: the customer's issue, steps taken, current status, and resolution 
        (if applicable). Be factual and concise.

User: Ticket subject: {subject}
      Messages:
      [{timestamp}] {senderName} ({role}): {content}
      ...
```

**BR-AI-005** The summary must not include speculative information beyond what is in the messages. The AI is instructed to stay factual.

**BR-AI-006** The response includes `modelUsed` (the Azure deployment name used, e.g., `gpt-4o-mini`) for audit and cost tracking.

**BR-AI-007** The generated summary is NOT persisted to the database — it is returned in the response only. Agents may copy it into an internal note manually. This avoids stale summaries as new messages arrive.

---

## POST /ai/tickets/{id}/suggest-reply

### Input Assembly
Fetches:
- Ticket context: same as summarize
- Request body: `tone` (`professional`, `friendly`, `formal`), `language` (`en`, `ar`)

### Prompt Design
```
System: You are a customer support agent. Write a reply in the {language} language 
        with a {tone} tone. Address the customer's most recent message. Sign off 
        with the agent's name.

User: Customer's most recent message: {latestCustomerMessage}
      Ticket context summary: {last 5 messages for context}
      Agent name: {callerFullName}
```

**BR-AI-008** Only the last customer message is the primary target; up to 5 prior messages are included as context (sliding window to manage token cost).

**BR-AI-009** The suggested reply is NOT automatically sent. The response body contains `suggestedReply` text which the agent can edit before using `POST /tickets/{id}/messages`.

**BR-AI-010** If `language = "ar"`, the prompt explicitly instructs Arabic output and the Azure deployment must support Arabic (use `gpt-4o` instead of `gpt-4o-mini` if Arabic quality is insufficient — deployment selection is configurable per feature via `appsettings.json`).

---

## POST /ai/tickets/{id}/suggest-category

### Input Assembly
Fetches:
- Ticket `Subject` and `Description`
- All active, leaf-level `TicketCategory` records (name, parentName) as the candidate list

### Prompt Design
```
System: You are a ticket classification assistant. Given a support ticket and a list 
        of categories, return the top 2 most likely categories as JSON. Format:
        [{"categoryId": "...", "confidence": 0.87}, ...]
        Confidence is between 0.0 and 1.0.

User: Subject: {subject}
      Description: {description}
      Available categories: [{categoryId: "...", label: "Parent → Child"}, ...]
```

**BR-AI-011** The AI returns raw confidence scores. The application maps them to confidence bands:
- `High`: confidence ≥ 0.80
- `Medium`: 0.50 ≤ confidence < 0.80
- `Low`: confidence < 0.50

**BR-AI-012** Up to 3 suggestions are returned, sorted by confidence descending. Suggestions with confidence < 0.20 are omitted (too uncertain to be useful).

**BR-AI-013** The agent must manually select and confirm a category via `PUT /tickets/{id}` (setting `categoryId`). The suggest-category endpoint does not change the ticket.

**BR-AI-014** The response includes only categories whose `categoryId` is in the current active category list. If the AI hallucinates a non-existent category ID, it is filtered out silently with a warning logged.

---

## POST /ai/tickets/{id}/suggest-articles

### Input Assembly
Fetches:
- Ticket `Subject`, `Description`, latest customer message
- All `Published`, `Public/Both` visibility `KbArticle` records: `id`, `Title`, `TitleAr`, `Content` (first 200 chars for relevance matching)

### Matching Approach (v1)
A prompt-based relevance approach is used in v1 (vector embeddings deferred to v2 due to Azure setup complexity):
```
System: You are a knowledge base search assistant. Return up to 5 article IDs 
        from the provided list that are most relevant to the customer's issue.
        Format: [{"articleId": "...", "relevanceScore": 0.92}, ...]

User: Customer issue: {subject} — {latestMessage}
      Articles: [{articleId, title, excerpt}, ...]
```

**BR-AI-015** If the article list exceeds the model's context window (e.g., 1000+ articles), a pre-filter is applied: SQL Server full-text search narrows to top 50 candidate articles before sending to the AI.

**BR-AI-016** `excerpt` returned in the response is the first 200 characters of the article's English content.

**BR-AI-017** Arabic article titles (`titleAr`) are included in the response for Arabic-language customers. The agent can share the appropriate language link.

---

## POST /ai/chat/message

### Session Management

**BR-AI-018** `sessionId = null` creates a new `ChatSession` record. The server returns the new `sessionId` in the response. All subsequent messages in the conversation must pass the same `sessionId`.

**BR-AI-019** `ChatSession` stores: `CustomerId` (or `UserId` for agent context), `Context` (`portal` or `agent`), `Status` (`Active`, `AgentConnected`, `Closed`), `CreatedAt`, `AgentId` (null until handoff).

**BR-AI-020** `ChatSessionMessage` stores each turn: `Role` (`user` or `assistant`), `Content`, `CreatedAt`. The full session history is sent to the AI on each message (sliding window: last 20 messages to manage tokens).

### Chatbot Logic

```
System (portal context): You are a customer support chatbot for {companyName}. 
    Help customers with their questions. If you cannot help after 3 attempts, 
    or if the topic is sensitive, or the customer asks for a human, 
    respond with the JSON flag: {"handoffRequired": true, "reason": "..."}.
    Otherwise respond naturally.

System (agent context): You are an AI assistant for support agents. 
    Help agents by answering questions about policies, drafting responses, 
    or looking up information. You have access to the KB article context provided.
```

**BR-AI-021** Handoff triggers (set `handoffRequired = true` in response):
1. Bot cannot understand after 3 consecutive messages from same session (tracked by `ChatSession.FailedUnderstandingCount`).
2. Customer message contains keywords: `human`, `agent`, `support person`, `real person`, `speak to someone` (case-insensitive, configurable list in `appsettings.json`).
3. `ChatSession.CreatedAt < now - 10 minutes` AND `ChatSession.Status != AgentConnected`.
4. AI provider detects sensitive topic (prompt instructs the model to self-identify).

**BR-AI-022** `suggestedArticles` in the chat response are populated when the AI response indicates a self-service option exists. Up to 3 articles suggested, pulled from the KB using the same relevance logic as `suggest-articles` (lightweight version: top 3 only).

**BR-AI-023** For `context = "agent"`, no `handoffRequired` logic applies (agent IS the human). Suggested articles are still returned.

### Error Handling

**BR-AI-024** If Azure OpenAI returns a 503 or rate-limit error, the endpoint returns `503` with code `AI_PROVIDER_UNAVAILABLE`. The error is logged with request details (excluding PII from the log body — ticket ID and session ID only).

**BR-AI-025** AI response timeout: 30 seconds. If no response within 30 seconds, return `503`. This is the maximum acceptable wait for an interactive feature.

**BR-AI-026** AI failures do not affect ticket integrity. All AI endpoints are side-effect-free — a failure at the AI layer does not modify any domain entities.

---

## Acceptance Criteria

**AC-AI-001** Given a ticket with zero messages, when any `/ai/tickets/{id}/*` endpoint is called, then the response is `422` with code `NO_MESSAGES_TO_PROCESS`.

**AC-AI-002** Given Azure OpenAI returns a response with a category ID not present in the active category list, when `suggest-category` processes the response, then that suggestion is silently filtered out and the remaining valid suggestions are returned.

**AC-AI-003** Given `suggest-reply` is called with `language = "ar"`, then the `suggestedReply` field contains Arabic text.

**AC-AI-004** Given a chat session with `context = "portal"` where the customer has sent 3 messages the bot could not understand, when the 4th message is sent, then the response includes `handoffRequired = true`.

**AC-AI-005** Given a customer message containing the word "human", then the chat response includes `handoffRequired = true` without waiting for 3 failed attempts.

**AC-AI-006** Given the Azure OpenAI endpoint is configured with a US-region URL, when the application starts, then it fails to start with a configuration error (not a runtime 503).

**AC-AI-007** Given a chat session is 11 minutes old and still in Active status, when the next message is processed, then `handoffRequired = true` is returned.

**AC-AI-008** Given `suggest-category` returns confidence scores [0.87, 0.62, 0.15], then the response includes 2 suggestions (the 0.15 is filtered as below 0.20 minimum).

---

## Edge Cases

- **Category list too large**: if > 100 active categories, send only the top 50 most recently created (as a proxy for relevance). Vector search (v2) will fix this properly.
- **Empty KB**: if no Published articles exist, `suggest-articles` returns `suggestions: []` without calling the AI (short-circuit).
- **Session ID mismatch**: if a Customer passes a `sessionId` that belongs to a different Customer, the response is `404` (not 403, to avoid session enumeration attacks).
- **Concurrent messages on same session**: if two requests arrive simultaneously for the same `sessionId`, only one is processed (optimistic lock on `ChatSession`). The second gets `409 Conflict` with code `SESSION_BUSY`.

---

## Integration Points

| AI Action | Downstream Effect |
|-----------|------------------|
| `suggest-category` returned | None (agent must manually apply) |
| `suggest-reply` returned | None (agent must manually send) |
| `suggest-articles` returned | None (agent must manually share) |
| `summarize` returned | None (agent may manually copy to internal note) |
| Chat `handoffRequired = true` | ChatHub (client initiates handoff → Ticket created from session) |
| Chat session created | `ChatSession` record persisted |
