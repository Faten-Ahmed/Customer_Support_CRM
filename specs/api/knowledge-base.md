# API Spec — Knowledge Base

> Base path: `/kb`

---

## GET /kb/articles

List KB articles. Visibility filtered by role: Agents see Internal + Public; Customers see Public only.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `status` | string | `Draft`, `Review`, `Published`, `Archived` |
| `visibility` | string | `Internal`, `Public`, `Both` |
| `categoryId` | uuid | Filter by linked ticket category |
| `authorId` | uuid | Filter by author |
| `search` | string | Full-text search on title and content |
| `page` | int | Default 1 |
| `pageSize` | int | Default 20 |

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "How to Reset Your Password",
      "titleAr": "كيفية إعادة تعيين كلمة المرور",
      "status": "Published",
      "visibility": "Both",
      "author": { "id": "uuid", "fullName": "Ahmed Al-Farsi" },
      "categoryId": "uuid",
      "viewCount": 142,
      "publishedAt": "2025-10-01T08:00:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 38, "totalPages": 2 }
}
```

---

## POST /kb/articles

Create a new article draft.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "title": "How to Reset Your Password",
  "titleAr": "كيفية إعادة تعيين كلمة المرور",
  "content": "## Steps\n1. Go to login page\n2. Click 'Forgot Password'...",
  "contentAr": "## الخطوات\n1. انتقل إلى صفحة تسجيل الدخول...",
  "visibility": "Both",
  "categoryId": "uuid",
  "tags": ["password", "account", "access"]
}
```

**Response 201:**
```json
{
  "data": {
    "id": "uuid",
    "title": "How to Reset Your Password",
    "status": "Draft",
    "createdAt": "2025-10-15T09:00:00Z"
  }
}
```

---

## GET /kb/articles/{id}

Get full article content.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "title": "How to Reset Your Password",
    "titleAr": "كيفية إعادة تعيين كلمة المرور",
    "content": "## Steps\n...",
    "contentAr": "## الخطوات\n...",
    "visibility": "Both",
    "status": "Published",
    "author": { "id": "uuid", "fullName": "Ahmed Al-Farsi" },
    "publishedBy": { "id": "uuid", "fullName": "Manager Name" },
    "categoryId": "uuid",
    "tags": ["password", "account"],
    "viewCount": 143,
    "createdAt": "2025-10-15T09:00:00Z",
    "publishedAt": "2025-10-15T10:00:00Z"
  }
}
```

**Errors:** `404` not found | `403` Internal article accessed by Customer role

---

## PUT /kb/articles/{id}

Update an article. Only the author, Manager, Admin, or designated Editor may edit.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** All fields from POST, all optional.

**Response 200:** Full article object (same as GET)

**Errors:** `403` not author/editor/manager/admin | `422` cannot edit a Published article directly — must archive first

---

## POST /kb/articles/{id}/submit-review

Move article from Draft → Review (author requests publication approval).

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** *(no body)*

**Response 200:**
```json
{ "data": { "id": "uuid", "status": "Review" } }
```

**Errors:** `422` article not in Draft status

---

## POST /kb/articles/{id}/publish

Approve and publish an article (Review → Published).

**Auth:** Bearer | **Roles:** `[Admin, Manager]` or designated Editor

**Request:** *(no body)*

**Response 200:**
```json
{ "data": { "id": "uuid", "status": "Published", "publishedAt": "2025-10-15T10:00:00Z" } }
```

**Errors:** `422` article not in Review status

---

## POST /kb/articles/{id}/archive

Archive a published article (Published → Archived).

**Auth:** Bearer | **Roles:** `[Admin, Manager]`

**Request:** *(no body)*

**Response 200:**
```json
{ "data": { "id": "uuid", "status": "Archived", "archivedAt": "2025-10-15T12:00:00Z" } }
```

---

## DELETE /kb/articles/{id}

Hard-delete a Draft article. Published/Archived articles must be archived first.

**Auth:** Bearer | **Roles:** `[Admin]`

**Response 204:** No content

**Errors:** `422` cannot delete non-Draft articles

---

## GET /kb/search

Full-text search across published articles visible to the caller's role.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `q` | string | Search query (required, min 2 chars) |
| `categoryId` | uuid | Restrict to category |
| `visibility` | string | `Internal`, `Public`, `Both` |
| `page` | int | Default 1 |
| `pageSize` | int | Default 10 |

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "How to Reset Your Password",
      "titleAr": "كيفية إعادة تعيين كلمة المرور",
      "excerpt": "...click 'Forgot Password' on the login page...",
      "visibility": "Both",
      "categoryId": "uuid",
      "publishedAt": "2025-10-01T08:00:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 10, "totalCount": 5, "totalPages": 1 }
}
```
