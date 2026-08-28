# US-BE-057 — Unread Notification Count (Redis Cache)

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


**Epic:** Notifications
**Roles:** Any authenticated user
**As a** user, **I want** the notification badge to load instantly, **so that** the UI is snappy even with many notifications.

## Acceptance Criteria
- [ ] `GET /notifications/unread-count` returns `{ "data": { "count": N } }` in < 100ms
- [ ] Count is served from Redis key `notifications:unread:{userId}` (TTL: 60 seconds)
- [ ] Cache is set/updated on: notification created, `read-all` called, single notification marked read
- [ ] Cache miss (first call or after invalidation): count is computed from DB and cached
- [ ] Count reflects only caller's unread notifications

## Technical Notes
- Endpoint: `GET /notifications/unread-count`
- Infrastructure: Redis `IDistributedCache` or `IConnectionMultiplexer`
- Business rule: BR-NOT-004
- Spec: `specs/api/notifications.md`

## Dependencies
- US-BE-053, US-BE-056
