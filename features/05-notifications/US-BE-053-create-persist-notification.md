# US-BE-053 — Create and Persist Notification

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
**Roles:** System (internal service)
**As the** system, **I want to** create a `Notification` record for a user when a relevant domain event fires, **so that** the user has a persistent inbox of alerts.

## Acceptance Criteria
- [ ] `NotificationService.CreateAsync(type, recipientId, entityType, entityId, title, body)` persists a `Notification` record
- [ ] Duplicate suppression for SLA types: if a notification with same `type` + `entityId` already exists → skip insert, return existing
- [ ] Supports all 18 notification types defined in `specs/features/05-notifications.md`
- [ ] Title/body use token substitution: `{{ticketNumber}}`, `{{customerName}}`, etc.
- [ ] Redis unread-count cache for recipient is invalidated after insert

## Technical Notes
- Implementation: `NotificationService` in Application layer, called from domain event handlers
- Entity: `Notification`
- Business rules: BR-NOT-001, BR-NOT-006
- Spec: `specs/features/05-notifications.md`

## Dependencies
- US-BE-007
