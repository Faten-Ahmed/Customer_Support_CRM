# US-BE-043 — SLA Policy CRUD

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


**Epic:** SLA Management
**Roles:** Admin (write), Admin + Manager (read)
**As an** admin, **I want to** configure SLA response and resolution time targets per priority, **so that** teams have defined service commitments.

## Acceptance Criteria
- [ ] `GET /admin/sla/policies` lists all policies (global + department-specific)
- [ ] `PUT /admin/sla/policies/{id}` updates `firstResponseMinutes`, `resolutionMinutes`, `updateFrequencyMinutes`, `warningThresholdPercent`, `breachThresholdPercent`, `criticalBreachThresholdPercent`
- [ ] `firstResponseMinutes` must be > 0 and < `resolutionMinutes`; violation returns `422`
- [ ] Threshold percents must satisfy `warning < breach < criticalBreach`; violation returns `422`
- [ ] Policy changes do NOT retroactively affect existing open tickets (snapshot isolation)
- [ ] Admin creates department-specific policy via `POST /admin/sla/policies` with `priority` + `departmentId`

## Technical Notes
- Endpoints: `GET /admin/sla/policies`, `PUT /admin/sla/policies/{id}`
- Entity: `SlaPolicy`
- Business rules: BR-ADM-027—030, BR-SLA-009
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-007
