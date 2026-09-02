# API Overview — AZM Squad Customer Support CRM

> **Status:** Draft — Phase 3
> **Version:** 0.1.0
> **Date:** 2026-08-25

---

## Base URL

```
Development:  http://localhost:5000/api/v1
Production:   https://crm.azmsquad.com/api/v1
```

All endpoints are prefixed with `/api/v1/`.

---

## Versioning

- Version is embedded in the URL path: `/api/v1/`
- Breaking changes increment the major version: `/api/v2/`
- Non-breaking additions (new fields, new endpoints) do not change the version

---

## Authentication

All endpoints except `POST /auth/login`, `POST /auth/portal/register`, `POST /auth/portal/verify-email`, `POST /auth/forgot-password`, and `POST /webhooks/*` require a valid JWT.

```
Authorization: Bearer <access_token>
```

### Token Lifecycle

| Token | TTL | Storage |
|-------|-----|---------|
| Access token | 15 minutes | Memory (Angular service) |
| Refresh token | 7 days | HttpOnly cookie |

### Token Claims

```json
{
  "sub": "user-guid",
  "name": "Ahmed Al-Farsi",
  "email": "ahmed@company.com",
  "role": "Agent",
  "primaryDepartmentId": "dept-guid",
  "departmentIds": ["dept-guid-1", "dept-guid-2"],
  "exp": 1234567890
}
```

---

## Authorization

Role hierarchy: `Admin (1) > Manager (2) > Agent (3) > Customer (4)`

Roles listed on each endpoint indicate **minimum role required**. Higher roles inherit all lower-role permissions unless explicitly restricted.

**Special notation:**
- `[Admin]` — Admin only
- `[Admin, Manager]` — Admin or Manager
- `[Agent+]` — Agent, Manager, or Admin
- `[Customer]` — Customer portal users only
- `[Any]` — Any authenticated user

---

## Request Format

```
Content-Type: application/json
Accept-Language: en   (or: ar)
```

All request bodies are JSON. `Accept-Language` controls the language of validation messages and system text in responses.

---

## Response Format

### Success

```json
{
  "data": { },
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8
  }
}
```

`meta` is only present on paginated list responses.

### Single Resource

```json
{
  "data": {
    "id": "uuid",
    "field": "value"
  }
}
```

### Error

```json
{
  "errors": [
    {
      "code": "VALIDATION_ERROR",
      "message": "Email is required.",
      "field": "email"
    }
  ]
}
```

**Standard error codes:**

| HTTP | Code | Meaning |
|------|------|---------|
| 400 | `VALIDATION_ERROR` | Request body failed validation |
| 401 | `UNAUTHORIZED` | Missing or expired token |
| 403 | `FORBIDDEN` | Authenticated but insufficient role/permission |
| 404 | `NOT_FOUND` | Resource does not exist |
| 409 | `CONFLICT` | Unique constraint violation (e.g., duplicate email) |
| 422 | `BUSINESS_RULE_VIOLATION` | Valid request but breaks a domain rule (e.g., invalid status transition) |
| 429 | `RATE_LIMITED` | Too many requests |
| 500 | `INTERNAL_ERROR` | Unexpected server error |

---

## Pagination

List endpoints support pagination via query parameters:

```
GET /api/v1/tickets?page=1&pageSize=20
```

| Parameter | Default | Max | Description |
|-----------|---------|-----|-------------|
| `page` | 1 | — | Page number (1-based) |
| `pageSize` | 20 | 100 | Items per page |

Response `meta` always includes `totalCount` and `totalPages`.

---

## Filtering & Sorting

List endpoints accept filter parameters specific to each resource. Common patterns:

```
GET /api/v1/tickets?status=InProgress&priority=Critical&departmentId=uuid
GET /api/v1/tickets?sortBy=createdAt&sortDir=desc
GET /api/v1/customers?search=ahmed
```

| Parameter | Description |
|-----------|-------------|
| `search` | Full-text search across key fields |
| `sortBy` | Field name to sort by (camelCase) |
| `sortDir` | `asc` or `desc` (default: `desc`) |

---

## Date & Time

- All timestamps in responses are **ISO 8601 UTC**: `2025-10-15T09:30:00Z`
- All timestamps in request bodies must be ISO 8601 (UTC or with offset)
- Dates-only (e.g., holiday dates) use `YYYY-MM-DD`
- The Angular client converts UTC to KSA time (UTC+3) for display

---

## IDs

All IDs are **UUID v4 strings** (lowercase with hyphens):
```
"id": "a4f2c8e1-3b7d-4f9a-8c2e-1d5b6e7f8a9b"
```

---

## File Uploads

Attachment uploads use `multipart/form-data` (not JSON):

```
Content-Type: multipart/form-data
```

Maximum file size enforced at the API gateway level (5 MB per file).
Allowed MIME types validated server-side: `application/pdf`, `image/jpeg`, `image/png`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `text/plain`.

---

## Real-Time (SignalR)

SignalR hubs are at:

```
ws://localhost:5000/hubs/notifications
ws://localhost:5000/hubs/chat
ws://localhost:5000/hubs/dashboard
```

Authentication: the JWT access token is passed as a query parameter on the WebSocket handshake:
```
?access_token=<token>
```

---

## API Modules

| File | Module |
|------|--------|
| `auth.md` | Login, token refresh, portal registration |
| `customers.md` | Customer profiles and contacts |
| `tickets.md` | Ticket lifecycle, messages, attachments, history, SLA |
| `knowledge-base.md` | KB articles and search |
| `agent-dashboard.md` | Agent tasks, quick-reply templates, availability |
| `admin.md` | Users, departments, branches, categories, field definitions |
| `notifications.md` | In-app notifications |
| `reports.md` | Reports and live dashboard |
| `customer-portal.md` | Customer self-service portal |
| `ai.md` | AI suggestions and chatbot |
| `communication.md` | Inbound channel webhooks |
