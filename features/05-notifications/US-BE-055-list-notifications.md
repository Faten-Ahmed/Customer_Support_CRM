# US-BE-055 — List Notifications

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
**As a** user, **I want to** view my notification inbox, **so that** I can catch up on events I missed.

## Acceptance Criteria
- [ ] `GET /notifications` returns caller's notifications, newest first; paginated (default 20, max 50)
- [ ] Filter: `?isRead=false` returns only unread; `?type=SlaWarning` filters by type
- [ ] Default date window: last 90 days; `?all=true` (Admin only) removes date filter
- [ ] Each notification includes: `id`, `type`, `title`, `body`, `entityType`, `entityId`, `isRead`, `createdAt`
- [ ] Returns only the caller's own notifications (no cross-user access)

## Technical Notes
- Endpoint: `GET /notifications`
- Entity: `Notification`
- Business rules: BR-NOT-003, BR-NOT-007
- Spec: `specs/api/notifications.md`

## Dependencies
- US-BE-053, US-BE-007
