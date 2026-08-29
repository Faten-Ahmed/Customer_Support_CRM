# Bilingual Column Audit Log

**Rule (AR-COL-001):** For every bilingual field pair, if the English column is required (`NOT NULL` / `Validators.required`), the Arabic column must also be required.

**Scope:** Feature 02 — Ticket Management, Feature 09 — Customer Portal  
**Audit date:** 2026-08-29  
**Auditor:** Claude Code

---

## Summary

| Feature | Area | Violations found | Violations fixed |
|---------|------|-----------------|-----------------|
| 02 — Ticket Management | Backend: Domain, Infrastructure, Application, API | 0 | — |
| 09 — Customer Portal | Backend: Domain, Application, API | 0 | — |
| 09 — Customer Portal | Frontend: Services, Components | **1** | **1** |

---

## Feature 02 — Ticket Management

### Backend

| Location | Field pair | EN required | AR required | Status |
|----------|-----------|-------------|-------------|--------|
| `CRM.Domain/Tickets/Ticket.cs` | `Subject` / `SubjectAr` | ✅ (`= null!`) | ✅ (`= null!`) | COMPLIANT |
| `CRM.Domain/Tickets/Ticket.cs` | `Description` / `DescriptionAr` | ✅ (`= null!`) | ✅ (`= null!`) | COMPLIANT |
| `CRM.Infrastructure/Persistence/Configurations/TicketConfiguration.cs` | `Subject` / `SubjectAr` | ✅ (`.IsRequired()`) | ✅ (`.IsRequired()`) | COMPLIANT |
| `CRM.Infrastructure/Persistence/Configurations/TicketConfiguration.cs` | `Description` / `DescriptionAr` | ✅ (`.IsRequired()`) | ✅ (`.IsRequired()`) | COMPLIANT |
| `CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs` | `Subject` / `SubjectAr` | ✅ (non-nullable param) | ✅ (non-nullable param) | COMPLIANT |
| `CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs` | `Description` / `DescriptionAr` | ✅ (non-nullable param) | ✅ (non-nullable param) | COMPLIANT |
| `CRM.Application/Tickets/Commands/UpdateTicketCommand.cs` | `Subject` / `SubjectAr` | ✅ (non-nullable param) | ✅ (non-nullable param) | COMPLIANT |
| `CRM.Application/Tickets/Commands/UpdateTicketCommand.cs` | `Description` / `DescriptionAr` | ✅ (non-nullable param) | ✅ (non-nullable param) | COMPLIANT |
| `CRM.API/Controllers/TicketsController.cs` — `CreateTicketRequest` | `Subject` / `SubjectAr` | ✅ (non-nullable) | ✅ (non-nullable) | COMPLIANT |
| `CRM.API/Controllers/TicketsController.cs` — `CreateTicketRequest` | `Description` / `DescriptionAr` | ✅ (non-nullable) | ✅ (non-nullable) | COMPLIANT |
| `CRM.API/Controllers/TicketsController.cs` — `UpdateTicketRequest` | `Subject` / `SubjectAr` | ✅ (non-nullable) | ✅ (non-nullable) | COMPLIANT |
| `CRM.API/Controllers/TicketsController.cs` — `UpdateTicketRequest` | `Description` / `DescriptionAr` | ✅ (non-nullable) | ✅ (non-nullable) | COMPLIANT |

---

## Feature 09 — Customer Portal

### Backend

| Location | Field pair | EN required | AR required | Status |
|----------|-----------|-------------|-------------|--------|
| `CRM.Application/Portal/Tickets/Commands/CreateTicketPortalCommand.cs` | `Subject` / `SubjectAr` | ✅ (non-nullable param) | ✅ (non-nullable param) | COMPLIANT |
| `CRM.Application/Portal/Tickets/Commands/CreateTicketPortalCommand.cs` | `Description` / `DescriptionAr` | ✅ (non-nullable param) | ✅ (non-nullable param) | COMPLIANT |
| `CRM.API/Controllers/Portal/PortalTicketsController.cs` — `CreatePortalTicketRequest` | `Subject` / `SubjectAr` | ✅ (non-nullable) | ✅ (non-nullable) | COMPLIANT |
| `CRM.API/Controllers/Portal/PortalTicketsController.cs` — `CreatePortalTicketRequest` | `Description` / `DescriptionAr` | ✅ (non-nullable) | ✅ (non-nullable) | COMPLIANT |
| `CRM.API/Controllers/Portal/PortalController.cs` — `UpdateProfileRequest` | `FullName` / `FullNameAr` | `string?` (optional) | `string?` (optional) | COMPLIANT (both optional — no violation) |

### Frontend

| Location | Field | EN required | AR required | Status |
|----------|-------|-------------|-------------|--------|
| `portal/submit-ticket/portal-submit-ticket.component.ts` | `subject` / `subjectAr` | ✅ (`Validators.required`) | ✅ (`Validators.required`) | COMPLIANT |
| `portal/submit-ticket/portal-submit-ticket.component.ts` | `description` / `descriptionAr` | ✅ (`Validators.required`) | ✅ (`Validators.required`) | COMPLIANT |
| `portal/auth/portal-register/portal-register.component.ts` | `fullName` / `fullNameAr` | ✅ (`Validators.required`) | ✅ (`Validators.required`) | COMPLIANT |
| `portal/services/portal-profile.service.ts` — `PortalProfile` interface | `fullName` / `fullNameAr` | ✅ (`string`) | ~~`string?`~~ → **`string`** | **FIXED** |

---

## Fixes Applied

### FIX-001 — `portal-profile.service.ts`: `PortalProfile.fullNameAr` made required

**File:** `src/frontend/src/app/portal/services/portal-profile.service.ts`

**Before:**
```typescript
export interface PortalProfile {
  id: string;
  fullName: string;
  fullNameAr?: string;   // VIOLATION
  email: string;
  ...
}
```

**After:**
```typescript
export interface PortalProfile {
  id: string;
  fullName: string;
  fullNameAr: string;    // FIXED
  email: string;
  ...
}
```

**Cascading changes:**

| File | Change |
|------|--------|
| `portal/services/portal-profile.service.spec.ts` | Added `fullNameAr` to both `req.flush(...)` mock objects |
| `portal/profile/portal-profile.component.spec.ts` | Added `fullNameAr: 'جين دو'` to `mockProfile` constant |
| `portal/profile/portal-profile.component.ts` | Removed now-redundant `?? ''` null-coalescing on `fullNameAr` in `ngOnInit` and `cancelEdit` |

**Tests after fix:** 271/271 pass (43 test files, 0 failures).
