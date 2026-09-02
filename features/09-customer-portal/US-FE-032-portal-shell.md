# US-FE-032 — Portal Shell (Layout & RTL Support)

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
**As a** customer, **I want** the portal to look professional and support both Arabic and English, **so that** it feels native to my preferred language.

## Acceptance Criteria
- [ ] Separate Angular lazy module `PortalModule` with its own routing at `/portal/**`
- [ ] Header: logo, nav links (My Tickets, Knowledge Base, My Profile), language toggle (EN/AR), user name + logout
- [ ] Language toggle: switches `dir="rtl"` on `<html>` and loads Arabic i18n strings (`@angular/localize`)
- [ ] Footer: contact info, portal version
- [ ] Responsive layout: mobile-friendly (hamburger nav on small screens)
- [ ] Unauthenticated routes: `/portal/login`, `/portal/register` — no header nav

## Technical Notes
- Component: `PortalShellComponent` with child `RouterOutlet`
- i18n: `@angular/localize` with `en` and `ar` locales; RTL CSS via Material `dir="rtl"` support
- Spec: `specs/features/09-customer-portal.md` BR-PLT-001

## Dependencies
- US-FE-002, US-FE-005
