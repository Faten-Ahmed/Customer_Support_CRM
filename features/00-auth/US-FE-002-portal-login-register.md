# US-FE-002 — Portal Login & Registration

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


**Epic:** Authentication
**Roles:** Customer
**As a** customer, **I want to** log in or register on the portal, **so that** I can access my support tickets.

## Acceptance Criteria
- [ ] Route: `/portal/login` — tabbed view with Login and Register tabs
- [ ] Login tab: email + password; on `401 EMAIL_NOT_VERIFIED` show "Please verify your email" with resend link
- [ ] Register tab: fullName, email, password, confirm password; on success show "Check your email to activate your account"
- [ ] RTL layout supported (direction toggles based on language setting)
- [ ] Client-side validation: email format, password ≥ 8 chars, passwords match
- [ ] Password visibility toggle on password fields

## Technical Notes
- Component: `PortalLoginComponent`, `PortalRegisterComponent`
- Service: `AuthService.portalLogin()`, `AuthService.portalRegister()`
- Spec: `specs/api/auth.md`, `specs/api/customer-portal.md`

## Dependencies
- US-BE-010, US-BE-094
