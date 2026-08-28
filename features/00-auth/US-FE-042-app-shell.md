# US-FE-042 — Internal App Shell & Navigation

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


**Epic:** Core
**Roles:** Admin, Manager, Agent
**As an** internal staff member, **I want** a consistent app shell with navigation, **so that** I can move between sections without confusion.

## Acceptance Criteria
- [ ] Left sidebar: Dashboard, Tickets, Customers, Knowledge Base, Reports, Admin (role-gated)
- [ ] Top bar: user name, role badge, notification bell, availability status (agents), logout
- [ ] Active route highlighted in sidebar
- [ ] Collapsed sidebar mode (icon-only) toggled by hamburger button; state persisted in localStorage
- [ ] 404 page for unknown routes; 403 page for unauthorised routes
- [ ] Loading bar (top progress indicator) during route transitions

## Technical Notes
- Component: `AppShellComponent` with `RouterOutlet`
- Angular: `@angular/router` with lazy-loaded feature modules
- Angular Material: `MatSidenav`, `MatToolbar`

## Dependencies
- US-FE-005, US-FE-019
