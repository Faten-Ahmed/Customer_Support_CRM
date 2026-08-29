# API Spec — AI Features

> Base path: `/ai`
> All AI features are advisory — they suggest, never auto-apply.
> Azure OpenAI (UAE/Europe region) required. Data residency mandatory.

---

## POST /ai/tickets/{id}/summarize

Generate a concise summary of a ticket's full thread.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** *(no body — ticket ID in path)*

**Response 200:**
```json
{
  "data": {
    "ticketId": "uuid",
    "summary": "Customer Sara reported being unable to log in after a password reset on Oct 14. Agent Ahmed reset the account and confirmed access was restored by Oct 15 at 11:30 AM. Customer confirmed resolution.",
    "generatedAt": "2025-10-15T11:00:00Z",
    "modelUsed": "gpt-4o-mini"
  }
}
```

**Errors:** `404` ticket not found | `503` AI provider unavailable | `422` ticket has no messages to summarize

---

## POST /ai/tickets/{id}/suggest-reply

Generate a suggested reply draft for the agent based on ticket context.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "tone": "professional",
  "language": "en"
}
```

Valid tones: `professional`, `friendly`, `formal`. Language: `en` or `ar`.

**Response 200:**
```json
{
  "data": {
    "ticketId": "uuid",
    "suggestedReply": "Hello Sara,\n\nThank you for your patience. I have successfully reset your account. Please try logging in again using your email address. If you encounter any further issues, please don't hesitate to reach out.\n\nBest regards,\nAhmed Al-Farsi\nTechnical Support",
    "language": "en",
    "generatedAt": "2025-10-15T10:45:00Z"
  }
}
```

Agent reviews and optionally edits before sending. Reply is not automatically posted.

**Errors:** `404` ticket not found | `503` AI unavailable

---

## POST /ai/tickets/{id}/suggest-category

Suggest a ticket category based on subject and description.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** *(no body)*

**Response 200:**
```json
{
  "data": {
    "ticketId": "uuid",
    "suggestions": [
      {
        "categoryId": "uuid",
        "categoryName": "Software",
        "parentCategoryName": "Technical Support",
        "confidence": 0.87,
        "confidenceBand": "High",
        "label": "Likely category: Technical Support → Software"
      },
      {
        "categoryId": "uuid",
        "categoryName": "Network",
        "parentCategoryName": "Technical Support",
        "confidence": 0.62,
        "confidenceBand": "Medium",
        "label": "Suggested category: Technical Support → Network"
      }
    ],
    "generatedAt": "2025-10-15T09:06:00Z"
  }
}
```

Confidence bands: `High` (>80%), `Medium` (50–80%), `Low` (<50%).
Agent must manually confirm the category — no auto-apply.

---

## POST /ai/tickets/{id}/suggest-articles

Suggest relevant knowledge base articles for an open ticket.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** *(no body)*

**Response 200:**
```json
{
  "data": {
    "ticketId": "uuid",
    "suggestions": [
      {
        "articleId": "uuid",
        "title": "How to Reset Your Password",
        "titleAr": "كيفية إعادة تعيين كلمة المرور",
        "relevanceScore": 0.92,
        "excerpt": "...click 'Forgot Password' on the login page..."
      },
      {
        "articleId": "uuid",
        "title": "Account Lockout Policy",
        "relevanceScore": 0.74,
        "excerpt": "...accounts are locked after 5 failed attempts..."
      }
    ],
    "generatedAt": "2025-10-15T09:07:00Z"
  }
}
```

Agent can share an article link with the customer or use it internally. No auto-send.

---

## POST /ai/chat/message

Send a message to the AI chatbot. Used by both the customer portal chatbot and the internal agent assistant.

**Auth:** Bearer | **Roles:** `[Any]`

**Request:**
```json
{
  "sessionId": "uuid",
  "message": "I can't log into my account",
  "context": "portal"
}
```

`context`: `portal` (customer-facing) or `agent` (internal assistant).
`sessionId`: ChatSession ID. Pass `null` to start a new session — server creates and returns one.

**Response 200:**
```json
{
  "data": {
    "sessionId": "uuid",
    "reply": "I'm sorry to hear you're having trouble logging in. Let me help! Could you tell me if you're seeing a specific error message?",
    "suggestedArticles": [
      {
        "articleId": "uuid",
        "title": "How to Reset Your Password",
        "url": "/portal/kb/uuid"
      }
    ],
    "handoffRequired": false,
    "handoffReason": null,
    "generatedAt": "2025-10-15T09:10:00Z"
  }
}
```

When `handoffRequired = true`, the client initiates agent handoff via SignalR ChatHub.

**Handoff triggers (set `handoffRequired = true`):**
- Bot cannot understand after 3 attempts
- Complex or sensitive topic detected
- Customer message contains "human", "agent", "support person"
- Session has been active > 10 minutes without resolution

**Errors:** `404` sessionId not found | `503` AI unavailable
