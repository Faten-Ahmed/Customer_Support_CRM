# Database Schema Change Log

---

## Change Set: Arabic Bilingual Columns + Users Table Rename

**Date:** 2026-08-28  
**Author:** FatenAhmed  
**EF Migration:** `20260828220747_AddArabicColumns`  
**Reason:** Bilingual (Arabic/English) support is a core product requirement — all customer-facing entities must carry an Arabic counterpart for display in RTL mode.

---

### What Changed

#### Table: Users (renamed from AspNetUsers)

| Column | Change | Type | Nullable |
|--------|--------|------|---------|
| `FirstNameAr` | **Added** | `NVARCHAR(100)` | YES |
| `LastNameAr` | **Added** | `NVARCHAR(100)` | YES |
| `JobTitle` | **Added** | `NVARCHAR(200)` | YES |
| `JobTitleAr` | **Added** | `NVARCHAR(200)` | YES |
| `FirstName` | Type tightened | `NVARCHAR(100)` | NO |
| `LastName` | Type tightened | `NVARCHAR(100)` | NO |
| `Email` | Type tightened | `NVARCHAR(256)` | NO |
| Unique index on `Email` | **Added** | — | — |

Table was previously referred to as `AspNetUsers` in FK constraint definitions throughout the schema. All FK references corrected to `Users`.

#### Table: Customers

| Column | Change | Type | Nullable |
|--------|--------|------|---------|
| `FullNameAr` | **Added** | `NVARCHAR(200)` | YES |
| `CompanyNameAr` | **Added** | `NVARCHAR(200)` | YES |

#### Table: Tickets

| Column | Change | Type | Nullable |
|--------|--------|------|---------|
| `SubjectAr` | **Added** | `NVARCHAR(500)` | YES |
| `DescriptionAr` | **Added** | `NVARCHAR(MAX)` | YES |

#### Table: TicketMessages (schema correction only, no migration)

| Column | Change |
|--------|--------|
| `ContentAr` | Corrected to `NULL` (was incorrectly documented as `NOT NULL`) |

---

### Why Nullable?

All Arabic fields are nullable because:
1. Existing data need not be back-filled.
2. Not all staff entering data may be bilingual.
3. UI falls back to English when Arabic field is empty.

---

### Files Updated

#### Infrastructure (Code — already implemented)

| File | Change |
|------|--------|
| `src/backend/src/CRM.Domain/Users/User.cs` | Added `FirstNameAr`, `LastNameAr`, `JobTitle`, `JobTitleAr` properties |
| `src/backend/src/CRM.Domain/Customers/Customer.cs` | Added `FullNameAr`, `CompanyNameAr` properties |
| `src/backend/src/CRM.Domain/Tickets/Ticket.cs` | Added `SubjectAr`, `DescriptionAr` properties |
| `src/backend/src/CRM.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | Created — was missing; configures Users table, all new columns |
| `src/backend/src/CRM.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs` | Added `FullNameAr`, `CompanyNameAr` property configs |
| `src/backend/src/CRM.Infrastructure/Persistence/Configurations/TicketConfiguration.cs` | Added `SubjectAr`, `DescriptionAr` property configs |
| `src/backend/src/CRM.Infrastructure/Migrations/20260828220747_AddArabicColumns.cs` | EF Core migration applying all column additions |

#### Specs & Documentation (Updated this change set)

| File | What changed |
|------|-------------|
| `specs/database/schema.md` | Added 8 new columns; fixed all `AspNetUsers` → `Users` FK refs; fixed `TicketMessages.ContentAr` nullability |
| `specs/domain/domain-model.md` | Updated 7 aggregates: User, Customer, Ticket, Notification, QuickReplyTemplate, AgentTask, CsatSurvey |
| `specs/api/auth.md` | `GET /auth/me` and `POST /auth/login` responses: split `fullName` → `firstName`/`lastName`, added bilingual + `requiresPasswordChange` |
| `specs/api/customers.md` | All customer DTOs: added `fullNameAr`, `companyNameAr` |
| `specs/api/tickets.md` | All ticket DTOs: added `subjectAr`, `descriptionAr` |
| `specs/api/admin.md` | User DTOs: split `fullName` → `firstName`/`lastName`, added `firstNameAr`, `lastNameAr`, `jobTitle`, `jobTitleAr` |
| `specs/api/customer-portal.md` | `GET /portal/profile`: added `fullNameAr`, `companyNameAr`; `PUT /portal/profile`: added `fullNameAr`; ticket endpoints: added `subjectAr`, `descriptionAr` |
| `features/01-customer-management/spec.md` | BR-CUST-010: documented `fullNameAr`, `companyNameAr` |
| `features/02-ticket-management/spec.md` | BR-TKT-003: documented `subjectAr`, `descriptionAr` |
| `features/07-admin-configuration/spec.md` | BR-ADM-002: documented split name fields + bilingual user fields |
| `features/00-auth/US-BE-001-login-internal.md` | Login response user object: split name, `requiresPasswordChange` |
| `features/00-auth/US-BE-006-get-current-user.md` | `/me` response: bilingual name + job title fields |
| `features/01-customer-management/US-BE-009-create-customer-internal.md` | Added `fullNameAr`, `companyNameAr` as optional |
| `features/01-customer-management/US-BE-012-get-customer.md` | Response includes `fullNameAr`, `companyNameAr` |
| `features/01-customer-management/US-BE-014-update-customer.md` | Updatable fields include `fullNameAr`, `companyNameAr` |
| `features/02-ticket-management/US-BE-019-create-ticket-internal.md` | Added `subjectAr`, `descriptionAr` as optional |
| `features/02-ticket-management/US-BE-021-get-ticket.md` | Response includes `subjectAr`, `descriptionAr` |
| `features/02-ticket-management/US-BE-023-update-ticket.md` | Updatable fields include `subjectAr`, `descriptionAr` |
| `features/07-admin-configuration/US-BE-063-create-internal-user.md` | Command uses `firstName`/`lastName` + bilingual fields |
| `features/07-admin-configuration/US-BE-064-get-list-update-users.md` | User DTOs use split name + bilingual fields |
| `features/01-customer-management/plans/US-BE-009-plan.md` | Customer aggregate uses `FullName`; added `FullNameAr`, `CompanyNameAr` to domain, DTOs, commands |
| `features/01-customer-management/plans/US-BE-012-plan.md` | CustomerDetailDto includes `FullNameAr`, `CompanyNameAr` |
| `features/01-customer-management/plans/US-BE-014-plan.md` | UpdateCustomerCommand includes `FullNameAr`, `CompanyNameAr` |
| `features/02-ticket-management/plans/US-BE-019-plan.md` | Ticket aggregate and CreateTicketCommand include `SubjectAr`, `DescriptionAr` |
| `features/02-ticket-management/plans/US-BE-021-plan.md` | TicketDetailDto includes `SubjectAr`, `DescriptionAr` |
| `features/02-ticket-management/plans/US-BE-023-plan.md` | UpdateTicketCommand includes `SubjectAr`, `DescriptionAr` |
| `features/07-admin-configuration/plans/US-BE-063-plan.md` | CreateInternalUserCommand uses `FirstName`/`LastName` + bilingual fields; removes FullName-split logic |
| `features/07-admin-configuration/plans/US-BE-064-plan.md` | UserProfileDto and UserListItemDto use split name + bilingual fields |
| `features/00-auth/plans/US-BE-001-plan.md` | LoginResponse DTO: `FullName` → `FirstName`, `LastName`; handler and controller updated |
| `features/01-customer-management/plans/US-BE-010-plan.md` | RegisterCustomerCommand: `FirstName`+`LastName` → `FullName`; validator, handler, tests updated |
| `features/01-customer-management/plans/US-BE-013-plan.md` | CustomerDto mapping: uses `FullName`, `FullNameAr`, `CompanyName`, `CompanyNameAr` |
| `features/02-ticket-management/plans/US-BE-020-plan.md` | CreateTicketPortalCommand: added `SubjectAr`, `DescriptionAr`; Customer.FullName used in handler |
| `features/09-customer-portal/plans/US-BE-080-plan.md` | PortalProfileDto: added `FullNameAr`, `CompanyNameAr`; UpdatePortalProfileCommand: added `FullNameAr` |
| `features/01-customer-management/plans/US-FE-006-customer-list.md` | `Customer` interface: added `fullNameAr?`, `companyNameAr?`; renamed `company` → `companyName` in interface, columns, and template |
| `features/01-customer-management/plans/US-FE-008-create-edit-customer-forms.md` | DTOs and form groups: added `fullNameAr?`, `companyNameAr?`; create/edit components wired |
| `features/07-admin-configuration/plans/US-FE-027-user-management.md` | `StaffUser` interface: `fullName` → `firstName`+`lastName`+bilingual fields; form dialog and display updated |
| `features/09-customer-portal/plans/US-FE-037-portal-profile.md` | `PortalProfile` interface: added `fullNameAr?`, `companyNameAr?`; form controls and template updated |

---

### Not Affected

The following features do not reference User name, Customer name, or Ticket subject/description directly in their DTOs and required no changes:

- Feature 03 (SLA Management)
- Feature 04 (Knowledge Base) — KbArticle already had bilingual Title/Content
- Feature 05 (Notifications) — Notification already had bilingual Title/Body in schema
- Feature 06 (Agent Dashboard)
- Feature 08 (Reports)
- Feature 09 (Customer Portal) — portal API spec updated; plan US-BE-080 updated (see above)
- Feature 10 (AI Features)
- Feature 11 (Communication Channels)
- Feature 12 (CSAT Surveys) — CsatSurvey already had CommentAr in schema

---

### Open Questions

None. All changes are greenfield (no existing data to migrate in dev environment).
