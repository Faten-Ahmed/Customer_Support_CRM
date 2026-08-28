# US-FE-008 — Create & Edit Customer Forms

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
**As an** agent, **I want to** create or edit a customer record via a form, **so that** the data is accurate.

## Acceptance Criteria
- [ ] Create form (dialog or page `/customers/new`): fullName (required), email (required), phone, companyName, country, city
- [ ] Edit form: same fields; email field is read-only (cannot change email — BR-CUST-002)
- [ ] On `409 EMAIL_ALREADY_EXISTS`: shows inline error on email field
- [ ] All required fields validated client-side before submit
- [ ] On success: navigates to customer detail page; shows success snackbar

## Technical Notes
- Components: `CreateCustomerFormComponent`, `EditCustomerFormComponent`
- Service: `CustomerService.create()`, `CustomerService.update()`
- Angular Reactive Forms

## Dependencies
- US-BE-009, US-BE-014, US-FE-006
