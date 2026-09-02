# US-FE-034 — Portal Submit Ticket Form

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
**As a** customer, **I want to** submit a new support ticket through the portal, **so that** I can get help without calling or emailing.

## Acceptance Criteria
- [ ] Route: `/portal/tickets/new`
- [ ] Form: department (dropdown), category (hierarchical — parent then child, optional), subject, description, custom fields (dynamically loaded)
- [ ] Custom fields rendered based on selected department; required fields marked; validated before submit
- [ ] On submit: navigates to new ticket detail page; success banner shown
- [ ] On `409 OPEN_TICKET_EXISTS`: shows "You already have an open ticket in this department — [link to existing ticket]"

## Technical Notes
- Component: `PortalSubmitTicketComponent`
- Services: `PortalTicketService.create()`, `PortalFieldDefinitionService.list(departmentId)`
- Spec: `specs/api/customer-portal.md`

## Dependencies
- US-BE-020, US-BE-070, US-FE-033
