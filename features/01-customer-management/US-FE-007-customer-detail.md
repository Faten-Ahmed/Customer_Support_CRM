# US-FE-007 — Customer Detail Page

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
**As an** agent, **I want to** view all information about a customer, **so that** I can provide informed support.

## Acceptance Criteria
- [ ] Route: `/customers/{id}`
- [ ] Header: full name, email, phone, company, country/city, VIP badge, Active status
- [ ] Tabs: Overview, Contacts, Tickets, Audit
- [ ] Overview tab: profile fields with inline edit button (Manager+ for VIP toggle)
- [ ] Contacts tab: lists contacts with add/remove controls
- [ ] Tickets tab: paginated ticket list for this customer (US-FE-012)
- [ ] "Deactivate Customer" button (Admin only) with confirmation dialog
- [ ] "New Ticket for Customer" shortcut button → pre-fills customerId in ticket form

## Technical Notes
- Component: `CustomerDetailComponent`
- Service: `CustomerService.getById()`
- Spec: `specs/api/customers.md`

## Dependencies
- US-BE-012, US-FE-006
