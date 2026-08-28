# US-BE-016 — Flag / Unflag VIP

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
**Roles:** Admin, Manager
**As a** manager, **I want to** mark a customer as VIP, **so that** agents are aware of their priority status.

## Acceptance Criteria
- [ ] `POST /customers/{id}/vip` sets `IsVip = true`; returns `200` with updated customer object
- [ ] `DELETE /customers/{id}/vip` sets `IsVip = false`; returns `200`
- [ ] Agent role calling either endpoint returns `403`
- [ ] `CustomerVipStatusChanged` domain event published
- [ ] Action logged in audit log

## Technical Notes
- Endpoints: `POST /customers/{id}/vip`, `DELETE /customers/{id}/vip`
- Entity: `Customer.IsVip`
- Business rule: BR-CUST-006
- Spec: `specs/api/customers.md`

## Dependencies
- US-BE-009, US-BE-007
