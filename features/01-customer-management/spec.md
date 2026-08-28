# Feature Spec — Customer Management

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


> Requirements: `REQ-CUST-*`
> API: `specs/api/customers.md`
> Domain entities: `Customer`, `CustomerContact`

---

## Overview

Customers are the end-users who submit support tickets. They exist independently of tickets and may be created by Admin/Agent (internal intake) or by self-registration through the Customer Portal. Each customer has a single login identity, a fixed profile schema, and an optional set of additional contacts.

---

## Business Rules

**BR-CUST-001** Every `Customer` record must have a unique email address (case-insensitive). Attempting to create or update to a duplicate email returns `409 Conflict`.

**BR-CUST-002** A Customer's email address cannot be changed after creation. It is the permanent identity key.

**BR-CUST-003** Customers created by Admin/Agent (internal creation) are active immediately — no email verification required.

**BR-CUST-004** Customers who self-register via the portal (`POST /auth/portal/register`) start in `EmailVerified = false` state and cannot log in until they verify their email.

**BR-CUST-005** Email verification tokens expire after 24 hours. A new token can be requested by re-submitting registration or a future resend-verification endpoint.

**BR-CUST-006** `IsVip` can only be toggled by Admin or Manager roles. VIP status has no automatic triggers; it is purely manual.

**BR-CUST-007** Customers are soft-deleted (`DeletedAt` timestamp set, `IsDeleted = true`). A soft-deleted customer's tickets, messages, and attachments are retained and remain accessible to internal staff.

**BR-CUST-008** A soft-deleted customer's email is freed for re-registration after deletion. However, if any active ticket references the deleted customer, deletion is blocked with `422 Unprocessable Entity` until all tickets are closed.

**BR-CUST-009** `CustomerContact` entries (additional contacts) are subordinate to the Customer. Creating or deleting contacts requires Agent+ role. A customer may have up to 10 additional contacts.

**BR-CUST-010** Searching customers (`GET /customers?search=`) matches against `FullName`, `Email`, `Phone`, `CompanyName` using a case-insensitive LIKE prefix or full-text search. Soft-deleted customers are excluded from search results by default; include them via `?includeDeleted=true` (Admin only).

**BR-CUST-011** `GET /customers/{id}/tickets` returns tickets for that customer scoped by the caller's role: Agent sees tickets in their departments only; Admin/Manager sees all.

---

## Workflows

### W-CUST-01: Internal Customer Creation
1. Agent or Admin submits `POST /customers` with required fields (`fullName`, `email`).
2. System validates uniqueness of `email` (case-insensitive).
3. Customer record created with `IsActive = true`, `EmailVerified = true` (skip portal verification), `CreatedBy = caller UserId`.
4. Domain event `CustomerCreated` published.
5. Response `201` with created object.

### W-CUST-02: Portal Self-Registration
1. Visitor submits `POST /auth/portal/register` with `fullName`, `email`, `password`.
2. System validates email uniqueness.
3. Customer record created with `IsActive = false`, `EmailVerified = false`.
4. Verification email dispatched (Hangfire job) with a 24-hour token link.
5. Response `202 Accepted` — no JWT issued yet.
6. Customer clicks link → `POST /auth/portal/verify-email` → `EmailVerified = true`, `IsActive = true`.
7. Customer can now log in via `POST /auth/login`.

### W-CUST-03: VIP Flagging
1. Manager or Admin calls `POST /customers/{id}/vip` or `DELETE /customers/{id}/vip`.
2. `IsVip` toggled. Audit log entry created.
3. Domain event `CustomerVipStatusChanged` published — downstream: SLA priority may be manually reviewed by manager (no automatic SLA override for VIP in v1).

### W-CUST-04: Soft Delete
1. Admin calls `DELETE /customers/{id}`.
2. System checks: any ticket for this customer has `Status` not in (`Closed`) → reject with `422`, listing the open ticket IDs.
3. If all tickets closed: `IsDeleted = true`, `DeletedAt = now`.
4. All active Customer portal sessions invalidated (JWT revoked via token blacklist or short TTL expiry).
5. Domain event `CustomerDeleted` published.

---

## Acceptance Criteria

**AC-CUST-001** Given a POST /customers request with an email that already exists (case-insensitive), when the endpoint is called, then the response is `409` with code `EMAIL_ALREADY_EXISTS`.

**AC-CUST-002** Given a portal-registered customer who has not verified their email, when they attempt to log in, then the response is `401` with code `EMAIL_NOT_VERIFIED`.

**AC-CUST-003** Given a customer with one open ticket (Status = InProgress), when an Admin calls DELETE /customers/{id}, then the response is `422` with a list of blocking ticket IDs.

**AC-CUST-004** Given an Agent calls POST /customers/{id}/vip, then the response is `403 Forbidden`.

**AC-CUST-005** Given a soft-deleted customer, when GET /customers is called without `includeDeleted=true`, then the customer does not appear in results.

**AC-CUST-006** Given a CustomerContact is created, when the parent Customer is soft-deleted, then all associated CustomerContacts are also soft-deleted in the same transaction.

**AC-CUST-007** Given a Customer with `EmailVerified = true` and `IsActive = true`, when they log in, then a valid JWT is returned.

---

## Edge Cases

- **Duplicate email on update**: `PUT /customers/{id}` cannot change email (field ignored or returns `422` if email field is supplied).
- **Expired verification token**: re-registering with the same email re-sends a new token and resets the 24-hour window. The old token is invalidated.
- **Contact count limit**: attempting to add an 11th contact returns `422` with code `MAX_CONTACTS_REACHED`.
- **Self-delete**: Customers cannot delete their own account via the portal (no endpoint exists for this in v1).

---

## Integration Points

| Event Published | Consumed By |
|----------------|-------------|
| `CustomerCreated` | Audit Log |
| `CustomerVipStatusChanged` | Audit Log, Notifications (manager) |
| `CustomerDeleted` | Auth (invalidate sessions), Audit Log |


## TASK: Reorganize Files into Feature-Based Nested Folders

I want to reorganize my project to have feature-based nested folders.

### New Structure (for each feature):
features/
├── customer-management/
│ ├── spec.md
│ ├── US-BE-009-create-customer.md
│ ├── US-BE-010-portal-register.md
│ ├── US-BE-011-email-verify.md
│ ├── US-BE-012-get-customer.md
│ ├── US-BE-013-list-customers.md
│ ├── US-BE-014-update-customer.md
│ ├── US-BE-015-soft-delete.md
│ ├── US-BE-016-vip-toggle.md
│ ├── US-BE-017-add-contact.md
│ ├── US-BE-018-delete-contact.md
│ ├── US-BE-096-ticket-history.md
│ ├── US-FE-006-customer-list.md
│ ├── US-FE-007-customer-detail.md
│ ├── US-FE-008-customer-forms.md
│ └── plans/
│ ├── US-BE-009-plan.md
│ ├── US-BE-010-plan.md
│ ├── US-BE-011-plan.md
│ ├── US-BE-012-plan.md
│ ├── US-BE-013-plan.md
│ ├── US-BE-014-plan.md
│ ├── US-BE-015-plan.md
│ ├── US-BE-016-plan.md
│ ├── US-BE-017-plan.md
│ ├── US-BE-018-plan.md
│ ├── US-BE-096-plan.md
│ ├── US-FE-006-plan.md
│ ├── US-FE-007-plan.md
│ └── US-FE-008-plan.md



### Steps:
1. Move `specs/features/01-customer-management.md` → `features/customer-management/spec.md`
2. Move `user-stories/backend/US-BE-009-*.md` → `features/customer-management/`
3. Move `user-stories/frontend/US-FE-006-*.md` → `features/customer-management/`
4. Move `plans/backend/US-BE-009-*.md` → `features/customer-management/plans/`
5. Move `plans/frontend/US-FE-006-*.md` → `features/customer-management/plans/`

Start with Customer Management, then Ticket Management, and so on.

After reorganizing, update all file references to reflect the new structure.