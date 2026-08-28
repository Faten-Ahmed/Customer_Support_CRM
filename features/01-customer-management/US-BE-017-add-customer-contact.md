# US-BE-017 — Add Customer Contact

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
**As an** agent, **I want to** add an additional contact person to a customer account, **so that** we can reach other people at the same organisation.

## Acceptance Criteria
- [ ] `POST /customers/{id}/contacts` with `name` (required), `email`, `phone`, `role` creates a `CustomerContact`; returns `201`
- [ ] Maximum 10 contacts per customer; exceeding returns `422` with code `MAX_CONTACTS_REACHED`
- [ ] `name` is required; missing returns `422`
- [ ] Contacts are returned in `GET /customers/{id}` response as `contacts[]`

## Technical Notes
- Endpoint: `POST /customers/{id}/contacts`
- Entity: `CustomerContact`
- Business rule: BR-CUST-009
- Spec: `specs/api/customers.md`

## Dependencies
- US-BE-009, US-BE-007
