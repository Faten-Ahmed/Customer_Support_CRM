# US-BE-038 — Custom Field Validation

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
**Roles:** System (used by US-BE-019, US-BE-020, US-BE-023)
**As the** system, **I want to** validate custom field values against their definitions, **so that** only valid data is stored.

## Acceptance Criteria
- [ ] On ticket create/update, load all active `TicketFieldDefinition` records for the ticket's `DepartmentId` (and `CategoryId` if set)
- [ ] For each `IsRequired = true` field: if no value in `customFieldValues` → error listing field name with code `REQUIRED_FIELD_MISSING`
- [ ] For `Dropdown` type: value must be in `options[]`; invalid value → `422`
- [ ] For `Number` type: value string must be parseable as a decimal; non-numeric → `422`
- [ ] For `Date` type: value must be valid ISO 8601 date; invalid → `422`
- [ ] For `Checkbox` type: value must be `"true"` or `"false"`; other values → `422`
- [ ] Unknown field IDs in `customFieldValues` (not in active definitions) → `422` listing unknown IDs
- [ ] All validation errors returned together (not fail-fast) in a single `422` response

## Technical Notes
- Implementation: `CustomFieldValidator` domain service, called from `CreateTicketCommandHandler` and `UpdateTicketCommandHandler`
- Entity: `TicketFieldDefinition`, `Ticket.CustomFieldValues` (JSON)
- Business rule: BR-TKT-005, BR-ADM-020—023
- Spec: `specs/features/02-ticket-management.md`, `specs/features/07-admin-configuration.md`

## Dependencies
- US-BE-073 (field definitions must exist first)
