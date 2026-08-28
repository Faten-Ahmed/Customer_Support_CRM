# US-FE-004 — First Login Password Change

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
**Roles:** Admin, Manager, Agent (newly created)
**As a** new staff member logging in for the first time, **I want to** be prompted to set a permanent password, **so that** my temporary password is replaced immediately.

## Acceptance Criteria
- [ ] Route: `/change-password` — accessible only when `423 PASSWORD_CHANGE_REQUIRED` is in effect; all other routes redirect here
- [ ] Form: current password, new password, confirm new password
- [ ] On success: redirects to `/dashboard`
- [ ] On `422 INVALID_CURRENT_PASSWORD`: shows inline error on current password field
- [ ] Enforces same password strength as registration (min 8 chars, uppercase, digit)

## Technical Notes
- Component: `ChangePasswordComponent`
- Service: `AuthService.changePassword()`
- Route guard: `PasswordChangeGuard` — checks if `passwordMustChange` flag is in decoded JWT
- Spec: `specs/api/auth.md`

## Dependencies
- US-BE-008, US-FE-001
