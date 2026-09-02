# Feature Spec — Knowledge Base

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


> Requirements: `REQ-KB-*`
> API: `specs/api/knowledge-base.md`, `specs/api/customer-portal.md` (portal KB endpoints)
> Domain entities: `KbArticle`, `KbCategory`

---

## Overview

The Knowledge Base (KB) is a bilingual (Arabic/English) repository of support articles. Articles go through a review lifecycle before being published. Visibility controls determine whether customers can see an article. Agents use the KB to send articles to customers and to resolve tickets faster; customers browse it for self-service.

---

## Article Lifecycle

```
[Draft] ──submit-review──▶ [PendingReview] ──approve──▶ [Published]
   ▲                              │                          │
   │                         reject (→ Draft)            archive
   └──────────────────────────────────────────────────▶ [Archived]
```

### Status Definitions

| Status | Who Can Set | Visible to Customers |
|--------|------------|---------------------|
| `Draft` | Author (Agent+) | No |
| `PendingReview` | Author (Agent+) via submit-review action | No |
| `Published` | Manager+ (approve action) | Yes (if visibility allows) |
| `Archived` | Manager+ | No |

**BR-KB-001** Only `Published` articles are returned to Customer Portal KB endpoints, filtered further by `Visibility`.

**BR-KB-002** Articles cannot skip the review step — the transition from `Draft` directly to `Published` is not permitted via the API (Admin may approve immediately if they are also the author).

**BR-KB-003** Rejecting a PendingReview article returns it to `Draft` status and records a rejection note (required, min 10 chars). The author receives a notification.

---

## Visibility

| Value | Internal (Agents) | Portal (Customers) |
|-------|----------------|--------------------|
| `Internal` | Yes | No |
| `Public` | Yes | Yes |
| `Both` | Yes | Yes |

`Both` is an alias for `Public` from the customer's perspective. The distinction is historical (reserved for future segmentation). In v1, treat `Both` the same as `Public` for portal access.

**BR-KB-004** `GET /portal/kb/articles` and `GET /portal/kb/search` return only articles where `Status = Published` AND `Visibility IN ('Public', 'Both')`.

**BR-KB-005** `GET /kb/articles` (agent-facing) returns all `Published` + optionally `Draft`/`PendingReview` depending on the caller's role and query filter. Customers have no access to the internal agent endpoint.

---

## Bilingual Content

Each article has:
- `Title` (English, required) + `TitleAr` (Arabic, optional)
- `Content` (Markdown, English, required) + `ContentAr` (Markdown, Arabic, optional)

**BR-KB-006** Arabic content is optional at creation but required before publishing if the Department serves Arabic-speaking customers (enforced at submission: if `Department.DefaultLanguage = 'ar'`, then `TitleAr` and `ContentAr` are required for `PendingReview` submission).

**BR-KB-007** Search (`GET /kb/search?q=`) searches both `Title` and `TitleAr`, and both `Content` and `ContentAr`. SQL Server full-text search configured on these columns.

---

## Search

**BR-KB-008** Full-text search uses SQL Server FTS. Minimum query length: 2 characters. Special characters are stripped before query execution.

**BR-KB-009** Search results are ranked by relevance (FTS rank score), then by `PublishedAt DESC` as a tiebreaker.

**BR-KB-010** `GET /portal/kb/search?q=` applies the same visibility filter as `GET /portal/kb/articles` — customers see only `Published + Public/Both` results.

---

## AI Integration

When an agent calls `POST /ai/tickets/{id}/suggest-articles`, the AI service:
1. Reads the ticket's `Subject`, `Description`, and most recent customer message.
2. Sends to Azure OpenAI for semantic similarity against published KB articles (vector search or prompt-based matching).
3. Returns up to 5 suggestions ranked by `relevanceScore`.

The agent may then share an article link with the customer or reference it internally. No automatic insertion into the ticket reply.

---

## Business Rules

**BR-KB-011** KB articles belong to a `KbCategory`. Categories are a flat list (no nesting in v1). An article must be assigned to a category.

**BR-KB-012** Only the original author or a Manager+ can edit a `Draft` or `PendingReview` article. A `Published` article can only be edited by Manager+.

**BR-KB-013** Editing a `Published` article automatically moves it back to `Draft`. The edited version must go through review again before becoming `Published`. The previous published version is not retained (no versioning in v1).

**BR-KB-014** `DELETE /kb/articles/{id}` hard-deletes `Draft` articles only. `Published` and `Archived` articles must be archived first.

**BR-KB-015** An `Archived` article cannot be moved back to `Draft` or `Published` — archive is a terminal state (except Admin can manually un-archive in v1 by calling a future endpoint; no un-archive endpoint exists in this spec).

---

## Workflows

### W-KB-01: Create and Publish Article
1. Agent calls `POST /kb/articles` → article created in `Draft`.
2. Agent adds/edits content. When ready: `POST /kb/articles/{id}/submit-review`.
3. Status → `PendingReview`. Manager/Admin receives `KbArticleSubmittedForReview` notification.
4. Manager reviews:
   - Approve: `POST /kb/articles/{id}/approve` → `Status = Published`, `PublishedAt = now`.
   - Reject: `POST /kb/articles/{id}/reject` with `rejectionNote` → `Status = Draft`. Author notified.
5. On `Published`: article becomes visible in agent KB and (if `Visibility = Public/Both`) in customer portal.

### W-KB-02: Archive Article
1. Manager+ calls `POST /kb/articles/{id}/archive`.
2. `Status = Archived`. Article no longer visible anywhere.
3. Any AI article suggestions referencing this article are stale — the AI suggest endpoint excludes `Archived` articles from its candidate pool.

---

## Acceptance Criteria

**AC-KB-001** Given an Agent calls POST /kb/articles and does not include `categoryId`, then the response is `422`.

**AC-KB-002** Given an article is in PendingReview, when an Agent (not the author) tries to edit it, then the response is `403 Forbidden`.

**AC-KB-003** Given a Published article is edited by a Manager, when the edit is saved, then the article Status becomes Draft and the article is no longer visible on the customer portal.

**AC-KB-004** Given `GET /portal/kb/articles`, when called by a Customer, then only `Published` articles with `Visibility IN (Public, Both)` are returned.

**AC-KB-005** Given a full-text search for "reset password" (`GET /kb/search?q=reset+password`), when two articles match — one with higher rank but older, one with lower rank but newer — then the higher-rank article appears first.

**AC-KB-006** Given a rejection of a PendingReview article without a `rejectionNote` in the request body, then the response is `422`.

**AC-KB-007** Given a Draft article, when `DELETE /kb/articles/{id}` is called by the author, then the article is permanently deleted and returns `204`.

**AC-KB-008** Given a Published article, when `DELETE /kb/articles/{id}` is called, then the response is `422` with code `MUST_ARCHIVE_FIRST`.

---

## Edge Cases

- **Concurrent edits**: last write wins (no optimistic concurrency in v1). Future consideration: add `ETag`/`If-Match` header support.
- **Category deletion**: if a KB category is deleted (or deactivated), articles assigned to it retain the category reference but are flagged `orphaned` in admin UI. They remain accessible but should be reassigned.
- **Search with Arabic**: Arabic right-to-left text is supported in both query and results. SQL Server FTS language = Arabic (2052 or configured locale).
- **Empty content article**: `Content` must be ≥ 100 characters for PendingReview submission (enforced at submit-review, not at create).

---

## Integration Points

| Event Published | Consumed By |
|----------------|-------------|
| `KbArticleSubmittedForReview` | Notifications (manager) |
| `KbArticlePublished` | AI (update article index for suggestions) |
| `KbArticleRejected` | Notifications (author) |
| `KbArticleArchived` | AI (remove from suggestion index) |
