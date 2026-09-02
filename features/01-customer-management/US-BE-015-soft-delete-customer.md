# US-BE-015 — Soft-Delete Customer

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


**Epic:** Customer Management
**Roles:** Admin
**As an** admin, **I want to** deactivate a customer record, **so that** they can no longer log in but their historical data is preserved.

## Acceptance Criteria
- [ ] `DELETE /customers/{id}` sets `IsDeleted = true`, `DeletedAt = now`
- [ ] Returns `422` with a list of blocking ticket IDs if the customer has any open tickets (Status not Closed)
- [ ] On success: all active portal JWT sessions for that customer are invalidated (refresh tokens revoked)
- [ ] `CustomerDeleted` domain event published
- [ ] Agent/Manager calling this endpoint returns `403`

## Technical Notes
- Endpoint: `DELETE /customers/{id}`
- Entity: `Customer`, `RefreshToken`
- Business rules: BR-CUST-007, BR-CUST-008
- Spec: `specs/api/customers.md`, `specs/features/01-customer-management.md` W-CUST-04

## Dependencies
- US-BE-009, US-BE-007
