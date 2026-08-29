# US-BE-014 — Update Customer Profile

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
**As an** agent, **I want to** update a customer's profile details, **so that** the record stays accurate.

## Acceptance Criteria
- [ ] `PUT /customers/{id}` accepts partial updates: `fullName`, `fullNameAr`, `phone`, `companyName`, `companyNameAr`, `country`, `city`
- [ ] `email` field is ignored if supplied (cannot change email — BR-CUST-002); no error is thrown, email is silently excluded
- [ ] Returns `200` with the updated customer object
- [ ] Updating a soft-deleted customer returns `404`

## Technical Notes
- Endpoint: `PUT /customers/{id}`
- Entity: `Customer`
- Business rule: BR-CUST-002
- Spec: `specs/api/customers.md`

## Dependencies
- US-BE-009, US-BE-007
