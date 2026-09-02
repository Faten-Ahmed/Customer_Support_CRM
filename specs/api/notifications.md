# API Spec — Notifications

> Base path: `/notifications`
> All endpoints scoped to the authenticated user.

---

## GET /notifications

List the caller's notifications, newest first.

**Auth:** Bearer | **Roles:** `[Any]`

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `isRead` | bool | `false` = unread only |
| `type` | string | Filter by NotificationType |
| `page` | int | Default 1 |
| `pageSize` | int | Default 20, max 50 |

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "type": "SlaWarning",
      "title": "SLA Warning — TKT-2025-00043",
      "body": "Ticket TKT-2025-00043 is at 80% of its resolution SLA. 30 minutes remaining.",
      "entityType": "Ticket",
      "entityId": "uuid",
      "isRead": false,
      "createdAt": "2025-10-15T10:30:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 5, "totalPages": 1 }
}
```

---

## GET /notifications/unread-count

Fast unread count for the notification badge.

**Auth:** Bearer | **Roles:** `[Any]`

**Response 200:**
```json
{ "data": { "count": 5 } }
```

---

## PUT /notifications/{id}/read

Mark a single notification as read.

**Auth:** Bearer | **Roles:** `[Any]`

**Request:** *(no body)*

**Response 200:**
```json
{ "data": { "id": "uuid", "isRead": true, "readAt": "2025-10-15T10:35:00Z" } }
```

**Errors:** `404` not found | `403` notification belongs to another user

---

## PUT /notifications/read-all

Mark all unread notifications for the caller as read.

**Auth:** Bearer | **Roles:** `[Any]`

**Request:** *(no body)*

**Response 200:**
```json
{ "data": { "markedRead": 5 } }
```

---

## SignalR — NotificationHub

Connected agents receive push notifications in real time without polling.

**Hub URL:** `ws://localhost:5000/hubs/notifications?access_token=<token>`

**Client methods (server → client):**

| Method | Payload |
|--------|---------|
| `ReceiveNotification` | Full notification object (same as list item) |
| `UnreadCountUpdated` | `{ "count": 6 }` |

No client → server methods needed (REST handles read marking).
