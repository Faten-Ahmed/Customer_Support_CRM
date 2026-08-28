# US-BE-009 — Create Customer (Internal)

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
**As an** agent, **I want to** create a customer record manually, **so that** I can open a ticket on behalf of a customer who contacted us by phone or walk-in.

## Acceptance Criteria
- [ ] `POST /customers` with `fullName` + `email` (required) returns `201` with the created customer object
- [ ] `EmailVerified = true` and `IsActive = true` — no verification needed for internal creation
- [ ] Duplicate email (case-insensitive) returns `409` with code `EMAIL_ALREADY_EXISTS`
- [ ] `phone`, `companyName`, `country`, `city` are optional
- [ ] `CustomerCreated` domain event published after successful creation
- [ ] Audit log entry written: action `CustomerCreated`, actorId = caller

## Technical Notes
- Endpoint: `POST /customers`
- Entity: `Customer`
- Business rules: BR-CUST-001, BR-CUST-003
- Spec: `specs/api/customers.md`, `specs/features/01-customer-management.md`

## Dependencies
- US-BE-007
