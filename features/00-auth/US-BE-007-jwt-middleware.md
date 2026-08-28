# US-BE-007 — JWT Middleware & Role Authorization

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
**As a** system, **I want** every API request validated against a JWT and role policy, **so that** unauthorized access is rejected at the infrastructure level.

## Acceptance Criteria
- [ ] All endpoints except `POST /auth/login`, `POST /auth/refresh`, `POST /auth/forgot-password`, `POST /auth/reset-password`, `POST /auth/portal/register`, `POST /auth/portal/verify-email`, and `POST /webhooks/*` require a valid Bearer token
- [ ] Missing token returns `401`; expired token returns `401` with code `TOKEN_EXPIRED`
- [ ] Wrong role for a guarded endpoint returns `403`
- [ ] `User.IsActive = false` checked on every request (even with a valid, non-expired token) → `401` with code `ACCOUNT_INACTIVE`
- [ ] Role hierarchy enforced: `[Agent+]` = Agent, Manager, Admin; `[Admin, Manager]` = those two only
- [ ] All `403` responses include the required role in the error body for debugging

## Technical Notes
- Implementation: ASP.NET Core `AuthenticationMiddleware` + policy-based authorization (`[Authorize(Policy = "AgentPlus")]` etc.)
- Spec: `specs/api/overview.md` (auth section)
- Business rule: BR-ADM-006

## Dependencies
- US-BE-001
