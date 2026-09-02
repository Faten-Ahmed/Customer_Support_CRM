# US-BE-069 — CRUD Ticket Categories

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


**Epic:** Admin Configuration
**Roles:** Admin (write), Admin + Manager (read)
**As an** admin, **I want to** manage a two-level category tree, **so that** tickets are classified consistently.

## Acceptance Criteria
- [ ] `GET /admin/categories` returns full tree: parents with nested `children[]`
- [ ] `POST /admin/categories` creates parent (no `parentId`) or child (`parentId` provided); returns `201`
- [ ] Max depth = 1: `parentId` pointing to an existing child returns `422` with code `MAX_DEPTH_EXCEEDED`
- [ ] `PUT /admin/categories/{id}` updates `name`, `nameAr`, `sortOrder`
- [ ] `POST /admin/categories/{id}/deactivate` also deactivates all children in same transaction; blocks if category has open tickets
- [ ] `POST /admin/categories/{id}/reactivate` reactivates only the parent (children must be re-activated individually)

## Technical Notes
- Endpoints: CRUD on `/admin/categories`
- Entity: `TicketCategory`
- Business rules: BR-ADM-016—019
- Spec: `specs/api/admin.md`

## Dependencies
- US-BE-007
