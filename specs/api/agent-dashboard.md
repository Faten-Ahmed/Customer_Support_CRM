# API Spec — Agent Dashboard

> Base path: `/agents`

---

## GET /agents/me/tickets

Tickets assigned to the authenticated agent, across all their departments.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:** `status`, `priority`, `departmentId`, `page`, `pageSize`, `sortBy`, `sortDir`

**Response 200:** Same shape as `GET /tickets` list, pre-filtered to caller's assigned tickets.

---

## PUT /agents/me/availability

Set the authenticated agent's availability status.

**Auth:** Bearer | **Roles:** `[Agent, Manager]`

**Request:**
```json
{ "status": "Busy" }
```

Valid values: `Available`, `Busy`, `Away`, `Offline`

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "availabilityStatus": "Busy",
    "lastAvailabilityChange": "2025-10-15T10:30:00Z"
  }
}
```

**Errors:** `400` invalid status value

---

## GET /agents/me/tasks

List the authenticated agent's personal tasks.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `status` | string | `Pending`, `InProgress`, `Completed` |
| `priority` | string | `High`, `Medium`, `Low` |
| `ticketId` | uuid | Tasks linked to a ticket |
| `customerId` | uuid | Tasks linked to a customer |
| `overdue` | bool | `true` = only overdue tasks |
| `page` | int | Default 1 |
| `pageSize` | int | Default 20 |

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "Follow up with Sara about access issue",
      "description": "Call at 2pm to confirm resolution.",
      "priority": "High",
      "status": "Pending",
      "dueAt": "2025-10-15T14:00:00Z",
      "isOverdue": false,
      "ticket": { "id": "uuid", "ticketNumber": "TKT-2025-00043" },
      "customer": { "id": "uuid", "fullName": "Sara Al-Mansouri" },
      "createdAt": "2025-10-15T09:30:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 4, "totalPages": 1 }
}
```

---

## POST /agents/me/tasks

Create a personal task.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "title": "Follow up with Sara about access issue",
  "description": "Call at 2pm to confirm resolution.",
  "priority": "High",
  "dueAt": "2025-10-15T14:00:00Z",
  "ticketId": "uuid",
  "customerId": "uuid"
}
```

**Response 201:** Created task object (same shape as list item)

---

## PUT /agents/me/tasks/{id}

Update a personal task.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** All POST fields optional; also accepts `status`.

**Response 200:** Updated task object

**Errors:** `404` task not found | `403` task belongs to another agent

---

## DELETE /agents/me/tasks/{id}

Delete a personal task.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 204:** No content

**Errors:** `404` not found | `403` not owner

---

## GET /agents/me/templates

List quick-reply templates visible to the authenticated agent (personal + global).

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:** `scope` (`Personal`, `Global`), `category`, `search`, `page`, `pageSize`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "Greeting — English",
      "content": "Hello {{customer_name}}, thank you for contacting {{department}} support.",
      "category": "Greeting",
      "scope": "Global",
      "createdBy": { "id": "uuid", "fullName": "Admin User" }
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 12, "totalPages": 1 }
}
```

---

## POST /agents/me/templates

Create a personal quick-reply template.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "title": "My Password Reset Response",
  "content": "Hi {{customer_name}}, I have reset your password. Please check your email.",
  "category": "Support"
}
```

**Response 201:** Created template object with `scope: "Personal"`

---

## PUT /agents/me/templates/{id}

Update a personal template.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** All POST fields optional.

**Response 200:** Updated template object

**Errors:** `403` cannot edit Global templates via this endpoint | `404` not found

---

## DELETE /agents/me/templates/{id}

Delete a personal template.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 204:** No content

**Errors:** `403` cannot delete Global templates | `404` not found

---

## POST /agents/me/templates/{id}/render

Render a template with placeholder values for a specific ticket context.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{ "ticketId": "uuid" }
```

**Response 200:**
```json
{
  "data": {
    "rendered": "Hi Sara Al-Mansouri, I have reset your password. Please check your email."
  }
}
```

**Errors:** `404` template or ticket not found
