# US-FE-003 — Forgot / Reset Password Flow

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
**Roles:** Any user
**As a** user who forgot my password, **I want to** reset it via email link, **so that** I can regain access.

## Acceptance Criteria
- [ ] `/forgot-password`: email field form; on submit shows success message regardless of whether email exists (no enumeration)
- [ ] `/reset-password?token=xxx`: new password + confirm password; on success shows "Password reset. Log in now." with link
- [ ] Token expired or invalid: shows clear error message with "Request a new link" button
- [ ] Password strength indicator shown as user types (weak / medium / strong)
- [ ] Client-side: passwords match validation before submit

## Technical Notes
- Components: `ForgotPasswordComponent`, `ResetPasswordComponent`
- Services: `AuthService.forgotPassword()`, `AuthService.resetPassword()`
- Spec: `specs/api/auth.md`

## Dependencies
- US-BE-004, US-BE-005
