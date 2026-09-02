# US-BE-018 — Remove Customer Contact

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
**As an** agent, **I want to** remove an additional contact from a customer account, **so that** outdated contacts don't clutter the record.

## Acceptance Criteria
- [ ] `DELETE /customers/{id}/contacts/{contactId}` hard-deletes the `CustomerContact` record; returns `204`
- [ ] Contact belonging to a different customer returns `404`
- [ ] Non-existent contact returns `404`

## Technical Notes
- Endpoint: `DELETE /customers/{id}/contacts/{contactId}`
- Entity: `CustomerContact`
- Spec: `specs/api/customers.md`

## Dependencies
- US-BE-017
