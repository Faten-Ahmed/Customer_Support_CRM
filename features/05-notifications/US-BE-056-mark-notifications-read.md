# US-BE-056 — Mark Notification(s) Read

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
**As a** user, **I want to** mark notifications as read, **so that** my unread badge reflects what I've actually seen.

## Acceptance Criteria
- [ ] `PUT /notifications/{id}/read` sets `IsRead = true`, `ReadAt = now`; returns `200` with updated notification
- [ ] Notification belonging to another user returns `403`
- [ ] Non-existent notification returns `404`
- [ ] `PUT /notifications/read-all` marks all caller's unread notifications as read; returns `{ "markedRead": N }`
- [ ] Redis unread-count cache for caller invalidated after both operations

## Technical Notes
- Endpoints: `PUT /notifications/{id}/read`, `PUT /notifications/read-all`
- Entity: `Notification`
- Business rules: BR-NOT-002, BR-NOT-003, BR-NOT-004
- Spec: `specs/api/notifications.md`

## Dependencies
- US-BE-053, US-BE-007
