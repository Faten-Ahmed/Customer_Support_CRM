# US-FE-037 — Portal Profile Page

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


**Epic:** Customer Portal
**Roles:** Customer
**As a** customer, **I want to** view and update my profile, **so that** my contact details are current.

## Acceptance Criteria
- [ ] Route: `/portal/profile`
- [ ] Displays: full name, email (read-only), phone, city
- [ ] Inline edit: click pencil icon → form fields become editable; Save/Cancel buttons appear
- [ ] Email shown with lock icon and tooltip "Email cannot be changed"
- [ ] On save: success snackbar; form reverts to read mode

## Technical Notes
- Component: `PortalProfileComponent`
- Service: `PortalProfileService.get()`, `PortalProfileService.update()`
- Spec: `specs/api/customer-portal.md`

## Dependencies
- US-BE-080, US-FE-032
