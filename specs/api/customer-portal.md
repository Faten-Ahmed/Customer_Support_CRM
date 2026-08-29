# API Spec — Customer Portal

> Base path: `/portal`
> All endpoints require Customer role JWT (obtained via POST /auth/login with customer credentials).

---

## GET /portal/profile

Get the authenticated customer's own profile.

**Auth:** Bearer | **Roles:** `[Customer]`

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "fullName": "Sara Al-Mansouri",
    "fullNameAr": "سارة المنصوري",
    "email": "sara@example.com",
    "phone": "+966501234567",
    "companyName": "Tech Corp",
    "companyNameAr": "تك كورب",
    "country": "Saudi Arabia",
    "city": "Riyadh"
  }
}
```

---

## PUT /portal/profile

Update own profile (limited fields — not email, not role).

**Auth:** Bearer | **Roles:** `[Customer]`

**Request:**
```json
{
  "fullName": "Sara Al-Mansouri",
  "fullNameAr": "سارة المنصوري",
  "phone": "+966501234568",
  "city": "Jeddah"
}
```

**Response 200:** Updated profile object (same shape as GET /portal/profile)

---

## GET /portal/tickets

List the authenticated customer's own tickets.

**Auth:** Bearer | **Roles:** `[Customer]`

**Query params:** `status`, `page`, `pageSize`, `sortBy`, `sortDir`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "ticketNumber": "TKT-2025-00043",
      "subject": "Cannot access system after password reset",
      "status": "InProgress",
      "priority": "High",
      "category": { "name": "Software", "parentName": "Technical Support" },
      "createdAt": "2025-10-15T09:05:00Z",
      "updatedAt": "2025-10-15T10:00:00Z",
      "sla": {
        "resolutionDeadline": "2025-10-16T09:05:00Z",
        "breachLevel": null
      }
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 8, "totalPages": 1 }
}
```

---

## POST /portal/tickets

Submit a new support ticket (web form — portal only, authenticated customer).

**Auth:** Bearer | **Roles:** `[Customer]`

**Request:**
```json
{
  "departmentId": "uuid",
  "categoryId": "uuid",
  "subject": "Cannot access system after password reset",
  "subjectAr": "لا أستطيع الوصول إلى النظام بعد إعادة تعيين كلمة المرور",
  "description": "Since yesterday I keep getting an error when trying to log in.",
  "descriptionAr": "منذ الأمس وأنا أحصل على خطأ عند محاولة تسجيل الدخول.",
  "customFieldValues": {
    "field-def-uuid": "value"
  }
}
```

Priority is set by the system (default: Medium). Channel is set to `Portal`.

**Response 201:**
```json
{
  "data": {
    "id": "uuid",
    "ticketNumber": "TKT-2025-00043",
    "status": "New",
    "createdAt": "2025-10-15T09:05:00Z"
  }
}
```

---

## GET /portal/tickets/{id}

Get a specific ticket. Customer can only see their own tickets.

**Auth:** Bearer | **Roles:** `[Customer]`

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "ticketNumber": "TKT-2025-00043",
    "subject": "Cannot access system after password reset",
    "subjectAr": "لا أستطيع الوصول إلى النظام بعد إعادة تعيين كلمة المرور",
    "description": "Since yesterday...",
    "descriptionAr": "منذ الأمس...",
    "status": "InProgress",
    "priority": "High",
    "category": { "name": "Software", "parentName": "Technical Support" },
    "assignedAgent": { "fullName": "Ahmed Al-Farsi" },
    "createdAt": "2025-10-15T09:05:00Z",
    "updatedAt": "2025-10-15T10:00:00Z",
    "sla": {
      "resolutionDeadline": "2025-10-16T09:05:00Z",
      "breachLevel": null
    },
    "messagesCount": 2
  }
}
```

**Errors:** `403` ticket belongs to another customer | `404` not found

---

## GET /portal/tickets/{id}/messages

Get customer-facing messages only (`isInternal = false`).

**Auth:** Bearer | **Roles:** `[Customer]`

**Query params:** `page`, `pageSize`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "senderName": "Ahmed Al-Farsi",
      "content": "Hi Sara, I have reset your account. Please try again.",
      "createdAt": "2025-10-15T09:20:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 2, "totalPages": 1 }
}
```

---

## POST /portal/tickets/{id}/messages

Customer replies to a ticket.

**Auth:** Bearer | **Roles:** `[Customer]`

**Request:**
```json
{ "content": "Still not working, same error appears." }
```

**Response 201:**
```json
{
  "data": {
    "id": "uuid",
    "content": "Still not working, same error appears.",
    "createdAt": "2025-10-15T09:30:00Z"
  }
}
```

**Errors:** `422` ticket is Closed (cannot reply to a closed ticket)

---

## POST /portal/tickets/{id}/close

Customer closes their own ticket.

**Auth:** Bearer | **Roles:** `[Customer]`

**Request:** *(no body)*

**Response 200:**
```json
{ "data": { "id": "uuid", "status": "Closed" } }
```

**Errors:** `422` ticket already closed

---

## GET /portal/kb/articles

Browse public knowledge base articles from the portal.

**Auth:** Bearer | **Roles:** `[Customer]`

**Query params:** `search`, `categoryId`, `page`, `pageSize`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "How to Reset Your Password",
      "titleAr": "كيفية إعادة تعيين كلمة المرور",
      "excerpt": "Follow these steps to reset your password...",
      "categoryId": "uuid",
      "publishedAt": "2025-10-01T08:00:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 10, "totalCount": 22, "totalPages": 3 }
}
```

Only `visibility = Public` or `Both` articles are returned.

---

## GET /portal/kb/articles/{id}

Read a public article.

**Auth:** Bearer | **Roles:** `[Customer]`

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "title": "How to Reset Your Password",
    "titleAr": "كيفية إعادة تعيين كلمة المرور",
    "content": "## Steps\n1. Go to the login page...",
    "contentAr": "## الخطوات\n1. انتقل إلى صفحة تسجيل الدخول...",
    "publishedAt": "2025-10-01T08:00:00Z"
  }
}
```

**Errors:** `403` article is Internal only | `404` not found

---

## GET /portal/kb/search

Search public knowledge base.

**Auth:** Bearer | **Roles:** `[Customer]`

**Query params:** `q` (required), `page`, `pageSize`

**Response 200:** Same shape as agent `GET /kb/search` but Public/Both visibility only.

---

## GET /portal/surveys/{id}

Get a pending CSAT survey for the customer.

**Auth:** Bearer | **Roles:** `[Customer]`

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "ticketNumber": "TKT-2025-00043",
    "ticketSubject": "Cannot access system after password reset",
    "sentAt": "2025-10-15T12:00:00Z",
    "isExpired": false
  }
}
```

**Errors:** `404` survey not found | `403` survey belongs to another customer

---

## POST /portal/surveys/{id}/submit

Submit CSAT survey response.

**Auth:** Bearer | **Roles:** `[Customer]`

**Request:**
```json
{
  "rating": 5,
  "comment": "Very fast and professional support. Resolved in under 3 hours!"
}
```

**Response 200:**
```json
{ "data": { "id": "uuid", "submittedAt": "2025-10-15T13:00:00Z" } }
```

**Errors:** `422` survey already submitted | `422` survey expired | `422` rating must be 1–5
