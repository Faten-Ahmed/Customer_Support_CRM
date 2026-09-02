# US-FE-001 — Login Page (Internal Staff)

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
**Roles:** Admin, Manager, Agent
**As a** support staff member, **I want to** log in with a clean, branded form, **so that** I can access the CRM from any browser.

## Acceptance Criteria
- [ ] Route: `/login` — redirect to `/dashboard` if already authenticated
- [ ] Form fields: `email` (required), `password` (required, masked)
- [ ] "Forgot password?" link navigates to `/forgot-password`
- [ ] On success: stores access token in memory (not localStorage); refresh token handled via HttpOnly cookie
- [ ] On `401 ACCOUNT_INACTIVE`: shows banner "Your account is deactivated. Contact your administrator."
- [ ] On `423 PASSWORD_CHANGE_REQUIRED`: redirects to `/change-password`
- [ ] Loading spinner on submit button while request is in flight
- [ ] Form validation: email format, both fields required (client-side, shown inline)

## Technical Notes
- Component: `LoginComponent` in `AuthModule`
- Service: `AuthService.login()`
- Angular Material: `MatFormField`, `MatInput`, `MatButton`
- Spec: `specs/api/auth.md` — POST /auth/login

## Dependencies
- US-BE-001
