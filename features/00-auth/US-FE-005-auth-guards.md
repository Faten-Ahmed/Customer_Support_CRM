# US-FE-005 — Auth Guards & HTTP Interceptor

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
**Roles:** All
**As the** frontend application, **I want to** protect routes by role and auto-refresh tokens silently, **so that** users are always authenticated without being interrupted.

## Acceptance Criteria
- [ ] `AuthGuard`: redirects unauthenticated users to `/login` (internal) or `/portal/login` (portal)
- [ ] `RoleGuard`: redirects to `/403` if the user's role doesn't match the required route roles
- [ ] `PasswordChangeGuard`: redirects to `/change-password` if `passwordMustChange = true`
- [ ] HTTP interceptor: attaches `Authorization: Bearer {token}` to all API requests
- [ ] On `401` response from any API call: attempts silent token refresh; on refresh failure → logout and redirect to login
- [ ] On `423 PASSWORD_CHANGE_REQUIRED`: stores redirect URL and navigates to `/change-password`

## Technical Notes
- Implementation: Angular `CanActivate` guards, `HttpInterceptor`
- Store: `AuthStore` (Angular Signal or NgRx) holding decoded JWT claims
- Services: `AuthService.refresh()`

## Dependencies
- US-BE-002, US-FE-001
