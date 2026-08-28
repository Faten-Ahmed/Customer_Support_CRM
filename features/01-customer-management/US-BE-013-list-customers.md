# US-BE-013 — List Customers

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
**Roles:** Admin, Manager, Agent
**As an** agent, **I want to** search and browse the customer list, **so that** I can quickly find a customer to view or link to a ticket.

## Acceptance Criteria
- [ ] `GET /customers` returns paginated list (`page`, `pageSize` default 20, max 50)
- [ ] `?search=` filters by `FullName`, `Email`, `Phone`, `CompanyName` (case-insensitive LIKE prefix)
- [ ] `?isVip=true` filters to VIP customers only
- [ ] `?isActive=false` filters to deactivated customers (Admin only; Agent/Manager receives `403`)
- [ ] Soft-deleted customers excluded by default; `?includeDeleted=true` requires Admin role
- [ ] Response includes `meta.totalCount`, `meta.totalPages`

## Technical Notes
- Endpoint: `GET /customers`
- Entity: `Customer`
- Business rule: BR-CUST-010
- Spec: `specs/api/customers.md`

## Dependencies
- US-BE-007, US-BE-009
