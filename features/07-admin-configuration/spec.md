# Feature Spec — Admin Configuration

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


> Requirements: `REQ-ADM-*`
> API: `specs/api/admin.md`
> Domain entities: `User`, `Department`, `Branch`, `TicketCategory`, `TicketFieldDefinition`, `QuickReplyTemplate`, `SlaPolicy`, `BusinessHours`, `Holiday`

---

## Overview

Admin Configuration covers all system setup and maintenance operations: user management, organizational structure (departments, branches), ticket taxonomy (categories, field definitions), SLA policies, business hours, and global reply templates. All endpoints require Admin role unless explicitly noted.

---

## User Management

### Roles & Constraints

**BR-ADM-001** Exactly 4 roles exist: `Admin`, `Manager`, `Agent`, `Customer`. Customer accounts are created via portal registration or customer-management endpoints — not through the admin user management endpoints (which are for internal staff only).

**BR-ADM-002** `POST /admin/users` requires `primaryDepartmentId` for `Agent` and `Manager` roles. `Admin` role has no department requirement.

**BR-ADM-003** A user's role cannot be changed via `PUT /admin/users/{id}`. Role changes require a separate workflow not in v1 scope (must deactivate and recreate). This prevents accidental privilege escalation.

**BR-ADM-004** An Admin cannot deactivate themselves. `POST /admin/users/{id}/deactivate` returns `422` with code `CANNOT_DEACTIVATE_SELF` if `id == caller.UserId`.

**BR-ADM-005** There must always be at least one active Admin. `POST /admin/users/{id}/deactivate` is rejected if the target is the last active Admin.

**BR-ADM-006** Deactivated users' JWT tokens become invalid on next use. On deactivation, the user's `IsActive = false` flag is checked at every API request; active JWT tokens are rejected with `401` even before expiry.

**BR-ADM-007** Department assignments for an Agent are managed via `PUT /admin/users/{id}/departments`. Exactly one department must have `isPrimary = true`. Minimum 1 department required.

**BR-ADM-008** Skill assignments (`PUT /admin/users/{id}/skills`) replace the full list atomically. Passing an empty `categoryIds` array removes all skills (allowed). Unknown category IDs return `422`.

### Password Management

**BR-ADM-009** When an Admin creates a user (`POST /admin/users`), they set a temporary password. The created user must change this password on first login (enforce via `PasswordMustChange = true` flag; API returns `423 Locked` with code `PASSWORD_CHANGE_REQUIRED` on all endpoints except `POST /auth/change-password` until changed).

---

## Departments

**BR-ADM-010** Department names must be unique (case-insensitive). Duplicate name returns `409`.

**BR-ADM-011** A Department cannot be deactivated if it has open tickets (`Status` not in `Closed`). Deactivation returns `422` with a count of blocking open tickets.

**BR-ADM-012** Deactivating a Department also deactivates it for auto-assignment (no new tickets routed there). Existing open tickets remain.

**BR-ADM-013** Each Department optionally references a `BusinessHours` record. If null, the global business hours apply.

---

## Branches

**BR-ADM-014** Branches are organizational groupings (e.g., Riyadh Branch, Jeddah Branch). They do not affect routing or SLA in v1 — they are for reporting and filtering only.

**BR-ADM-015** A Customer can be associated with a Branch. This is optional and informational only.

---

## Ticket Categories

**BR-ADM-016** Categories are two-level: a root category (parent) can have children. Children cannot have children (max depth = 1). Enforced by `CHECK` constraint: if `ParentCategoryId IS NOT NULL`, then no other category may reference this ID as `ParentCategoryId`.

**BR-ADM-017** Deactivating a parent category automatically deactivates all its children in the same transaction.

**BR-ADM-018** An active category that has open tickets assigned to it cannot be deactivated. Returns `422` listing the blocking ticket count.

**BR-ADM-019** `sortOrder` controls display order in agent and portal UI. Categories with the same `sortOrder` are sorted alphabetically.

---

## Ticket Field Definitions

Field definitions extend the ticket schema per department.

**BR-ADM-020** A `TicketFieldDefinition` is scoped to a `DepartmentId`. It may optionally also be scoped to a `CategoryId` within that department — making it appear only for tickets in that specific category.

**BR-ADM-021** Field types: `Text`, `Number`, `Date`, `Dropdown`, `Checkbox`. The `options` JSON array is required only for `Dropdown` type; must contain 2–20 items.

**BR-ADM-022** `DELETE /admin/field-definitions/{id}` is a soft-deactivate (sets `IsActive = false`). Existing `Ticket.CustomFieldValues` data for this field is retained and readable. New tickets will not prompt for the deactivated field.

**BR-ADM-023** `sortOrder` controls the display sequence of fields in the ticket creation form.

---

## Global Quick Reply Templates

**BR-ADM-024** Global templates are created by Admin and visible to all agents. They are identified by `scope = Global`.

**BR-ADM-025** Admins can edit or delete Global templates. Agents cannot.

**BR-ADM-026** Template variable support is the same as personal templates: `{{customer_name}}`, `{{agent_name}}`, `{{ticket_number}}`, `{{department}}`.

---

## SLA Policies

**BR-ADM-027** SLA policies are defined per `Priority` (Critical, High, Medium, Low) × optional `DepartmentId`. The system looks up the most specific applicable policy (Department-specific first, then global).

**BR-ADM-028** `PUT /admin/sla/policies/{id}` updates a policy's time values. Changes take effect for tickets created after the update only (snapshots on existing tickets are not changed — see `specs/features/03-sla-management.md`).

**BR-ADM-029** All SLA time values must be > 0 minutes. `firstResponseMinutes` must be < `resolutionMinutes`. Violating either returns `422`.

**BR-ADM-030** Warning and breach threshold percents must satisfy: `warningThresholdPercent < breachThresholdPercent < criticalBreachThresholdPercent`. Defaults: 80 / 100 / 200.

---

## Business Hours

**BR-ADM-031** One global `BusinessHours` record always exists (seeded on first deployment). It cannot be deleted.

**BR-ADM-032** Department-specific `BusinessHours` records are created as overrides. Each department can have at most one `BusinessHours` record.

**BR-ADM-033** `workDays` must contain 1–7 days. Attempting to set 0 work days returns `422`.

**BR-ADM-034** `timeZone` must be a valid IANA time zone string (e.g., `Asia/Riyadh`). Unrecognized time zone strings return `422` with code `INVALID_TIMEZONE`.

**BR-ADM-035** Holidays are per `BusinessHours` record. A holiday on a non-work-day is technically redundant but allowed (it has no additional effect).

**BR-ADM-036** Duplicate holiday dates (same date on the same `BusinessHours` record) return `409`.

---

## Workflows

### W-ADM-01: Onboard New Agent
1. Admin calls `POST /admin/users` with role `Agent`, email, temp password, `primaryDepartmentId`.
2. System creates user with `PasswordMustChange = true`.
3. Admin optionally calls `PUT /admin/users/{id}/departments` to add secondary departments.
4. Admin optionally calls `PUT /admin/users/{id}/skills` to assign category skills.
5. Agent receives welcome email (Hangfire: `SendWelcomeEmailJob`) with login URL and temp password.
6. Agent logs in → API returns `423` with `PASSWORD_CHANGE_REQUIRED` → agent calls `POST /auth/change-password` → `PasswordMustChange = false` → normal access granted.

### W-ADM-02: Decommission a Department
1. Verify no open tickets in department (`GET /reports/tickets?departmentId=X&status=open` → must be 0).
2. Reassign all Agents to other departments.
3. Call `POST /admin/departments/{id}/deactivate`.
4. Department no longer appears in dropdown lists or routing logic.

---

## Acceptance Criteria

**AC-ADM-001** Given the last active Admin account, when `POST /admin/users/{id}/deactivate` is called, then the response is `422` with code `CANNOT_DEACTIVATE_LAST_ADMIN`.

**AC-ADM-002** Given a Department override SLA policy is updated (firstResponseMinutes = 10), when a ticket is created after the update, then the ticket's snapshotted SLA uses 10 minutes — and an older ticket's SLA snapshot is unchanged.

**AC-ADM-003** Given `PUT /admin/users/{id}/departments` is called with two departments both having `isPrimary: true`, then the response is `422` with code `MULTIPLE_PRIMARY_DEPARTMENTS`.

**AC-ADM-004** Given a Dropdown field definition is created with 1 option, then the response is `422` (minimum 2 options required).

**AC-ADM-005** Given a Category has 3 open tickets, when `POST /admin/categories/{id}/deactivate` is called, then the response is `422` listing 3 blocking tickets.

**AC-ADM-006** Given a parent category is deactivated, then all its child categories are also deactivated in the same response.

**AC-ADM-007** Given a new user is created, when they log in for the first time and call `GET /tickets`, then the response is `423` with code `PASSWORD_CHANGE_REQUIRED`.

**AC-ADM-008** Given `POST /admin/business-hours/{id}/holidays` is called with the same date twice, then the second call returns `409`.

**AC-ADM-009** Given `PUT /admin/sla/policies/{id}` with `firstResponseMinutes = 100` and `resolutionMinutes = 50`, then the response is `422` (first response must be less than resolution).

---

## Edge Cases

- **Admin self-deactivation**: caught by `CANNOT_DEACTIVATE_SELF` rule before the last-admin check.
- **Category with mixed active/inactive children**: deactivating the parent deactivates all children regardless of their current state. Re-activating the parent does NOT re-activate children — they must be re-activated individually.
- **Field definition `sortOrder` conflict**: duplicate `sortOrder` values are allowed; display order breaks ties alphabetically.
- **Business hours spanning midnight**: `startTime > endTime` is NOT supported in v1. Business hours are same-day only (e.g., 08:00–18:00). 24/7 operations should set startTime=00:00, endTime=23:59 on all 7 days.

---

## Integration Points

| Action | Downstream Effect |
|--------|------------------|
| User deactivated | Auth (JWT invalidation), Ticket (unassign if agent has open tickets — manager notified) |
| Department deactivated | Routing (no new tickets), Reports (historical data preserved) |
| Category deactivated | Ticket creation (category no longer selectable), AI suggest-category |
| SLA policy updated | SLA (new tickets only) |
| Business hours updated | SLA (affects deadline calculations for new tickets) |
