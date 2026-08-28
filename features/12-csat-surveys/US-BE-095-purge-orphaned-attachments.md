# US-BE-095 — Purge Orphaned Attachments & Completed Tasks (Maintenance Jobs)

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


**Epic:** System Maintenance
**Roles:** System (Hangfire recurring jobs)
**As the** system, **I want to** clean up soft-deleted attachments and old completed tasks, **so that** storage and database stay lean.

## Acceptance Criteria
- [ ] `PurgeOrphanedAttachmentsJob` runs nightly: finds `TicketAttachment` where `DeletedAt IS NOT NULL AND DeletedAt < now - 1 day`; deletes S3 objects; hard-deletes DB records
- [ ] `PurgeCompletedTasksJob` runs nightly: finds `AgentTask` where `IsCompleted = true AND UpdatedAt < now - 30 days`; hard-deletes records
- [ ] Both jobs are idempotent; safe to re-run
- [ ] S3 delete failures are logged and skipped (DB record retained for next run)

## Technical Notes
- Implementation: Hangfire recurring jobs (nightly, e.g., `0 2 * * *`)
- Business rules: BR-TKT-008, BR-AGT-018
- Spec: `specs/features/02-ticket-management.md`, `specs/features/06-agent-dashboard.md`

## Dependencies
- US-BE-031, US-BE-062
