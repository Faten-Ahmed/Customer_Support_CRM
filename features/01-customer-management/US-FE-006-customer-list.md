# US-FE-006 — Customer List Page

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
**As an** agent, **I want to** browse and search the customer list, **so that** I can find a customer quickly.

## Acceptance Criteria
- [ ] Route: `/customers`
- [ ] Table with columns: Full Name, Email, Phone, Company, VIP badge, Active status, Tickets count, Created date
- [ ] Search bar: filters by name/email/phone/company in real time (debounced 300ms)
- [ ] Filter chips: VIP only, Active only
- [ ] VIP customers highlighted with a badge
- [ ] Clicking a row navigates to `/customers/{id}`
- [ ] "New Customer" button (Admin/Manager/Agent) opens create form
- [ ] Pagination: page controls with total count
- [ ] Empty state message when no results

## Technical Notes
- Component: `CustomerListComponent` in `CustomersModule`
- Service: `CustomerService.list()`
- Spec: `specs/api/customers.md` — GET /customers

## Dependencies
- US-BE-013, US-FE-005
