# US-FE-033 — Portal Ticket List, Detail & Reply

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
**As a** customer, **I want to** view my tickets and reply to agents, **so that** I can track and progress my support requests.

## Acceptance Criteria
- [ ] Route: `/portal/tickets` — card-list view: Ticket #, Subject, Status badge, Priority, Updated date; filter by status; no table (card UX is friendlier)
- [ ] Route: `/portal/tickets/{id}` — subject, status, assigned agent first name, message thread (customer-facing messages only), reply box, close button
- [ ] Reply box: text only (no internal note toggle); submit sends message
- [ ] If ticket is Resolved and customer replies: success message "Your reply has reopened the ticket"
- [ ] If ticket is Closed: reply box hidden; "Ticket Closed" banner shown
- [ ] "Close Ticket" button: confirmation dialog → closes ticket → shows CSAT survey prompt if survey available

## Technical Notes
- Components: `PortalTicketListComponent`, `PortalTicketDetailComponent`
- Services: `PortalTicketService.list()`, `PortalTicketService.getById()`, `PortalTicketService.addMessage()`, `PortalTicketService.close()`
- Spec: `specs/api/customer-portal.md`

## Dependencies
- US-BE-020, US-BE-028, US-BE-081, US-FE-032
