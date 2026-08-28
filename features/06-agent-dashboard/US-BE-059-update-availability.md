# US-BE-059 — Update Availability Status

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


**Epic:** Agent Dashboard
**Roles:** Agent
**As an** agent, **I want to** set my availability status (Available, Busy, Away, Offline), **so that** the system knows whether to assign new tickets to me.

## Acceptance Criteria
- [ ] `PUT /agents/me/availability` with `{ "status": "Busy" }` updates `User.AvailabilityStatus`; returns `200`
- [ ] Valid values: `Available`, `Busy`, `Away`, `Offline`; invalid returns `422`
- [ ] Setting to `Busy`, `Away`, or `Offline` stops new auto-assignments to this agent (BR-AGT-008)
- [ ] Setting back to `Available` does NOT auto-assign pending unassigned tickets — agent must pull or system assigns on next `TicketCreated`
- [ ] `AgentStatusChanged` event published → DashboardHub `AgentWorkloadUpdated` push (US-BE-082)

## Technical Notes
- Endpoint: `PUT /agents/me/availability`
- Entity: `User.AvailabilityStatus`
- Business rules: BR-AGT-008, BR-AGT-010
- Spec: `specs/api/agent-dashboard.md`

## Dependencies
- US-BE-007
