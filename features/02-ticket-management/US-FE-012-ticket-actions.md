# US-FE-012 — Ticket Status Badge & Action Modals

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


**Epic:** Ticket Management
**Roles:** Admin, Manager, Agent
**As an** agent, **I want to** take key actions on a ticket (assign, escalate, transfer, change status) via modal dialogs, **so that** I don't leave the ticket detail page.

## Acceptance Criteria
- [ ] Status badge: colour-coded (New=grey, Assigned=blue, InProgress=green, OnHold=yellow, Escalated=red, Resolved=teal, Closed=dark)
- [ ] "Assign" modal: agent dropdown (filtered to active agents in current department)
- [ ] "Transfer" modal: department dropdown + transfer note textarea (required, min 10 chars)
- [ ] "Escalate" modal: reason textarea (required, min 20 chars)
- [ ] "Change Status" dropdown: shows only valid next states per current status and role
- [ ] "Resolve" flow: status modal with resolution text field (required, min 10 chars)
- [ ] Invalid transitions disabled/hidden (not just server-rejected)

## Technical Notes
- Components: `AssignModalComponent`, `TransferModalComponent`, `EscalateModalComponent`, `StatusChangeModalComponent`
- Services: `TicketService.assign()`, `TicketService.transfer()`, `TicketService.escalate()`, `TicketService.changeStatus()`

## Dependencies
- US-BE-024, US-BE-025, US-BE-026, US-BE-027, US-FE-010
