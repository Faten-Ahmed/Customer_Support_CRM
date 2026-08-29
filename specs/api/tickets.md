# API Spec — Ticket Management

> Base path: `/tickets`

---

## GET /tickets

List tickets. Scope filtered automatically by role:
- Admin/Manager: all tickets in their department(s)
- Agent: only tickets assigned to them in their department(s)

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `status` | string | `New`, `Assigned`, `InProgress`, `OnHold`, `Escalated`, `Resolved`, `Reopened`, `Closed` |
| `priority` | string | `Critical`, `High`, `Medium`, `Low` |
| `departmentId` | uuid | Filter by department |
| `assignedAgentId` | uuid | Filter by agent |
| `customerId` | uuid | Filter by customer |
| `categoryId` | uuid | Filter by category |
| `channel` | string | `Email`, `WhatsApp`, `SMS`, `LiveChat`, `WebForm`, `Portal` |
| `isEscalated` | bool | Escalated tickets only |
| `search` | string | Search in subject and description |
| `dateFrom` | date | Created on or after |
| `dateTo` | date | Created on or before |
| `page` | int | Default 1 |
| `pageSize` | int | Default 20, max 100 |
| `sortBy` | string | `createdAt`, `updatedAt`, `priority`, `status` |
| `sortDir` | string | `asc` / `desc` |

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "ticketNumber": "TKT-2025-00042",
      "subject": "Cannot access system",
      "status": "InProgress",
      "priority": "High",
      "channel": "Email",
      "isEscalated": false,
      "customer": { "id": "uuid", "fullName": "Sara Al-Mansouri", "isVip": false },
      "assignedAgent": { "id": "uuid", "fullName": "Ahmed Al-Farsi" },
      "department": { "id": "uuid", "name": "Technical Support" },
      "category": { "id": "uuid", "name": "Software", "parentName": "Technical Support" },
      "sla": {
        "firstResponseDeadline": "2025-10-15T09:30:00Z",
        "resolutionDeadline": "2025-10-15T17:00:00Z",
        "breachLevel": null
      },
      "createdAt": "2025-10-15T09:00:00Z",
      "updatedAt": "2025-10-15T10:00:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 142, "totalPages": 8 }
}
```

---

## POST /tickets

Create a new ticket.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "customerId": "uuid",
  "departmentId": "uuid",
  "categoryId": "uuid",
  "priority": "High",
  "subject": "Cannot access system after password reset",
  "subjectAr": "لا أستطيع الوصول إلى النظام بعد إعادة تعيين كلمة المرور",
  "description": "Customer reports being locked out since yesterday.",
  "descriptionAr": "يبلغ العميل عن عدم تمكنه من الدخول منذ الأمس.",
  "channel": "Email",
  "customFieldValues": {
    "field-def-uuid-1": "SN-12345",
    "field-def-uuid-2": "v2.4.1"
  }
}
```

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

**Errors:** `404` customer/department/category not found | `422` categoryId must be a child category | `422` required custom field missing

---

## GET /tickets/{id}

Get full ticket detail including SLA status, messages count, and custom fields.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "ticketNumber": "TKT-2025-00043",
    "subject": "Cannot access system after password reset",
    "subjectAr": "لا أستطيع الوصول إلى النظام بعد إعادة تعيين كلمة المرور",
    "description": "Customer reports being locked out since yesterday.",
    "descriptionAr": "يبلغ العميل عن عدم تمكنه من الدخول منذ الأمس.",
    "status": "InProgress",
    "priority": "High",
    "channel": "Email",
    "isEscalated": false,
    "escalationLevel": 0,
    "customer": {
      "id": "uuid", "fullName": "Sara Al-Mansouri",
      "email": "sara@example.com", "phone": "+966501234567", "isVip": false
    },
    "assignedAgent": { "id": "uuid", "fullName": "Ahmed Al-Farsi" },
    "department": { "id": "uuid", "name": "Technical Support", "nameAr": "الدعم الفني" },
    "category": { "id": "uuid", "name": "Software", "parentName": "Technical Support" },
    "customFieldValues": {
      "field-def-uuid-1": "SN-12345",
      "field-def-uuid-2": "v2.4.1"
    },
    "sla": {
      "firstResponseDeadline": "2025-10-15T09:30:00Z",
      "resolutionDeadline": "2025-10-15T17:00:00Z",
      "firstResponseAt": "2025-10-15T09:20:00Z",
      "isPaused": false,
      "breachLevel": null
    },
    "messagesCount": 3,
    "attachmentsCount": 1,
    "createdAt": "2025-10-15T09:05:00Z",
    "updatedAt": "2025-10-15T10:00:00Z",
    "firstResponseAt": "2025-10-15T09:20:00Z",
    "resolvedAt": null,
    "closedAt": null
  }
}
```

**Errors:** `404` ticket not found | `403` agent not in ticket's department

---

## PUT /tickets/{id}

Update editable ticket fields. Does NOT change status (use `/status`) or assignment (use `/assign`).

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "subject": "Updated subject",
  "subjectAr": "الموضوع المحدث",
  "description": "Updated description",
  "descriptionAr": "الوصف المحدث",
  "categoryId": "uuid",
  "priority": "Critical",
  "customFieldValues": {
    "field-def-uuid-1": "SN-99999"
  }
}
```

**Response 200:** Full ticket object (same as GET /tickets/{id})

**Errors:** `403` only assigned agent, Manager, or Admin may update | `422` invalid category

---

## DELETE /tickets/{id}

Soft-delete a ticket. Admin only.

**Auth:** Bearer | **Roles:** `[Admin]`

**Response 204:** No content

---

## POST /tickets/{id}/assign

Assign or reassign a ticket to an agent.

**Auth:** Bearer | **Roles:** `[Admin, Manager]`

**Request:**
```json
{ "agentId": "uuid" }
```

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "status": "Assigned",
    "assignedAgent": { "id": "uuid", "fullName": "Ahmed Al-Farsi" }
  }
}
```

**Errors:** `422` agent not in ticket's department | `422` agent status is Offline

---

## POST /tickets/{id}/status

Change ticket status. Enforces the transition matrix from OQ-006.

**Auth:** Bearer | **Roles:** depends on transition (see matrix)

**Request:**
```json
{
  "status": "Resolved",
  "reason": "Issue fixed — password reset and access confirmed by customer."
}
```

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "status": "Resolved",
    "resolvedAt": "2025-10-15T11:30:00Z"
  }
}
```

**Errors:** `422` transition not allowed from current status | `403` role not permitted for this transition

---

## POST /tickets/{id}/transfer

Transfer ticket to another department. Manager/Admin only.

**Auth:** Bearer | **Roles:** `[Admin, Manager]`

**Request:**
```json
{
  "toDepartmentId": "uuid",
  "reason": "Billing issue — transferring to Billing department."
}
```

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "departmentId": "uuid",
    "assignedAgentId": null
  }
}
```

Clears assignment on transfer. Reason is mandatory.

**Errors:** `422` same department | `422` reason not provided

---

## POST /tickets/{id}/escalate

Manually escalate a ticket. Raises priority by one level.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{ "reason": "Customer is a VIP and has been waiting for 2 hours." }
```

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "priority": "Critical",
    "isEscalated": true,
    "escalationLevel": 1
  }
}
```

**Errors:** `422` already at max escalation | `422` reason not provided

---

## GET /tickets/{id}/messages

Get all messages on a ticket. Agents see all; customers see only `isInternal = false`.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:** `page`, `pageSize`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "senderId": "uuid",
      "senderName": "Ahmed Al-Farsi",
      "senderEmail": null,
      "content": "Hi Sara, I can see the issue — resetting your account now.",
      "isInternal": false,
      "channel": "Email",
      "mentionedUsers": [],
      "createdAt": "2025-10-15T09:20:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 3, "totalPages": 1 }
}
```

---

## POST /tickets/{id}/messages

Post a new message on a ticket. Sets `firstResponseAt` on first agent outbound message.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "content": "Hi Sara, resetting your account now. @Ahmed please review.",
  "isInternal": false,
  "mentionedUserIds": ["uuid-of-ahmed"]
}
```

**Response 201:**
```json
{
  "data": {
    "id": "uuid",
    "content": "Hi Sara, resetting your account now. @Ahmed please review.",
    "isInternal": false,
    "mentionedUsers": [{ "id": "uuid", "fullName": "Ahmed Al-Farsi" }],
    "createdAt": "2025-10-15T09:20:00Z"
  }
}
```

**Errors:** `422` mentionedUserIds contains users not in this department

---

## GET /tickets/{id}/attachments

List all non-deleted attachments on a ticket.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "fileName": "error_screenshot.png",
      "contentType": "image/png",
      "fileSizeBytes": 245760,
      "uploadedBy": { "id": "uuid", "fullName": "Sara Al-Mansouri" },
      "uploadedAt": "2025-10-15T09:10:00Z",
      "expiresAt": "2027-10-15T09:10:00Z",
      "downloadUrl": "https://presigned-s3-url..."
    }
  ]
}
```

`downloadUrl` is a pre-signed S3 URL valid for 15 minutes.

---

## POST /tickets/{id}/attachments

Upload one or more files to a ticket.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** `multipart/form-data`
```
files[]: <binary>
files[]: <binary>
```

**Response 201:**
```json
{
  "data": [
    {
      "id": "uuid",
      "fileName": "error_screenshot.png",
      "fileSizeBytes": 245760,
      "uploadedAt": "2025-10-15T09:10:00Z"
    }
  ]
}
```

**Errors:** `400` unsupported file type | `413` file exceeds 5 MB | `422` ticket total would exceed 10 MB | `422` customer total would exceed 50 MB

---

## DELETE /tickets/{id}/attachments/{attachmentId}

Soft-delete a ticket attachment.

**Auth:** Bearer | **Roles:** `[Admin, Manager]`

**Response 204:** No content

**Errors:** `404` attachment not found

---

## GET /tickets/{id}/history

Full immutable history — status changes and department transfers in chronological order.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 200:**
```json
{
  "data": {
    "statusHistory": [
      {
        "id": "uuid",
        "fromStatus": null,
        "toStatus": "New",
        "changedBy": { "id": "uuid", "fullName": "Ahmed Al-Farsi" },
        "changedAt": "2025-10-15T09:05:00Z",
        "reason": null
      },
      {
        "id": "uuid",
        "fromStatus": "New",
        "toStatus": "Assigned",
        "changedBy": { "id": "uuid", "fullName": "Manager Name" },
        "changedAt": "2025-10-15T09:08:00Z",
        "reason": null
      }
    ],
    "departmentHistory": [
      {
        "id": "uuid",
        "fromDepartment": { "id": "uuid", "name": "Technical Support" },
        "toDepartment": { "id": "uuid", "name": "Billing" },
        "transferredBy": { "id": "uuid", "fullName": "Manager Name" },
        "transferredAt": "2025-10-15T10:00:00Z",
        "reason": "Billing issue confirmed"
      }
    ]
  }
}
```

---

## GET /tickets/{id}/sla

Current SLA clock status for a ticket.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 200:**
```json
{
  "data": {
    "priority": "High",
    "firstResponseDeadline": "2025-10-15T09:35:00Z",
    "resolutionDeadline": "2025-10-16T09:05:00Z",
    "firstResponseAt": "2025-10-15T09:20:00Z",
    "firstResponseMet": true,
    "resolutionMet": null,
    "isPaused": false,
    "totalPausedMinutes": 0,
    "breachLevel": null,
    "minutesUntilBreach": 1425
  }
}
```

---

## GET /tickets/unassigned

Tickets in the agent pull queue — not yet assigned, in the agent's departments.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:** `departmentId`, `priority`, `page`, `pageSize`

**Response 200:** Same shape as `GET /tickets` list.
