# API Spec — Admin Configuration

> Base path: `/admin`
> All endpoints require `[Admin]` unless noted.

---

## USERS

### GET /admin/users
List all system users.

**Roles:** `[Admin]`

**Query params:** `role`, `departmentId`, `isActive`, `search`, `page`, `pageSize`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "firstName": "Ahmed",
      "lastName": "Al-Farsi",
      "firstNameAr": "أحمد",
      "lastNameAr": "الفارسي",
      "jobTitle": "Senior Support Agent",
      "email": "ahmed@company.com",
      "role": "Agent",
      "primaryDepartment": { "id": "uuid", "name": "Technical Support" },
      "availabilityStatus": "Available",
      "isActive": true,
      "createdAt": "2025-09-01T08:00:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 18, "totalPages": 1 }
}
```

### POST /admin/users
Create a new internal user (Admin, Manager, or Agent).

**Request:**
```json
{
  "firstName": "Ahmed",
  "lastName": "Al-Farsi",
  "firstNameAr": "أحمد",
  "lastNameAr": "الفارسي",
  "jobTitle": "Senior Support Agent",
  "jobTitleAr": "موظف دعم أول",
  "email": "ahmed@company.com",
  "password": "TempPass123!",
  "role": "Agent",
  "primaryDepartmentId": "uuid"
}
```

**Response 201:** Created user object

**Errors:** `409` email exists | `422` primaryDepartmentId required for Agent/Manager role

### GET /admin/users/{id}
Get full user profile including all department assignments and skills.

**Response 200:**
```json
{
  "data": {
    "id": "uuid",
    "firstName": "Ahmed",
    "lastName": "Al-Farsi",
    "firstNameAr": "أحمد",
    "lastNameAr": "الفارسي",
    "jobTitle": "Senior Support Agent",
    "jobTitleAr": "موظف دعم أول",
    "email": "ahmed@company.com",
    "role": "Agent",
    "primaryDepartment": { "id": "uuid", "name": "Technical Support" },
    "departments": [
      { "id": "uuid", "name": "Technical Support", "isPrimary": true },
      { "id": "uuid", "name": "General Inquiry", "isPrimary": false }
    ],
    "skills": [
      { "categoryId": "uuid", "categoryName": "Hardware" },
      { "categoryId": "uuid", "categoryName": "Software" }
    ],
    "isActive": true
  }
}
```

### PUT /admin/users/{id}
Update user profile (not role change — use separate endpoint).

**Request:**
```json
{
  "firstName": "Ahmed",
  "lastName": "Al-Farsi",
  "firstNameAr": "أحمد",
  "lastNameAr": "الفارسي",
  "jobTitle": "Lead Support Agent",
  "jobTitleAr": "موظف دعم رئيسي",
  "primaryDepartmentId": "uuid"
}
```

**Response 200:** Updated user object

### POST /admin/users/{id}/deactivate
Deactivate a user (soft-delete).

**Response 200:**
```json
{ "data": { "id": "uuid", "isActive": false } }
```

**Errors:** `422` cannot deactivate the last active Admin

### POST /admin/users/{id}/reactivate
Reactivate a deactivated user.

**Response 200:**
```json
{ "data": { "id": "uuid", "isActive": true } }
```

### PUT /admin/users/{id}/departments
Replace the full list of department assignments for an agent.

**Request:**
```json
{
  "departments": [
    { "departmentId": "uuid", "isPrimary": true },
    { "departmentId": "uuid", "isPrimary": false }
  ]
}
```

**Response 200:** Updated user object with departments list

**Errors:** `422` exactly one department must have isPrimary = true

### PUT /admin/users/{id}/skills
Replace the full list of skill (category) assignments for an agent.

**Request:**
```json
{ "categoryIds": ["uuid", "uuid"] }
```

**Response 200:** Updated user object with skills list

---

## DEPARTMENTS

### GET /admin/departments
**Roles:** `[Admin, Manager]`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Technical Support",
      "nameAr": "الدعم الفني",
      "description": "Handles all technical issues",
      "businessHours": { "id": "uuid", "startTime": "08:00", "endTime": "18:00" },
      "isActive": true,
      "agentCount": 5
    }
  ]
}
```

### POST /admin/departments

**Request:**
```json
{
  "name": "Technical Support",
  "nameAr": "الدعم الفني",
  "description": "Handles all technical issues",
  "businessHoursId": "uuid"
}
```

**Response 201:** Created department object

### PUT /admin/departments/{id}

**Request:** All POST fields optional.

**Response 200:** Updated department object

### POST /admin/departments/{id}/deactivate / /reactivate

**Response 200:**
```json
{ "data": { "id": "uuid", "isActive": false } }
```

---

## BRANCHES

### GET /admin/branches / POST /admin/branches / PUT /admin/branches/{id}

Same pattern as departments. Branch object: `{ id, name, nameAr, isActive }`. No business hours override at branch level.

---

## TICKET CATEGORIES

### GET /admin/categories
Returns full category tree (parents with nested children).

**Roles:** `[Admin, Manager]`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Technical Support",
      "nameAr": "الدعم الفني",
      "isActive": true,
      "sortOrder": 1,
      "children": [
        { "id": "uuid", "name": "Hardware", "nameAr": "الأجهزة", "isActive": true, "sortOrder": 1 },
        { "id": "uuid", "name": "Software", "nameAr": "البرامج", "isActive": true, "sortOrder": 2 }
      ]
    }
  ]
}
```

### POST /admin/categories
Create a parent or child category.

**Request:**
```json
{
  "name": "Hardware",
  "nameAr": "الأجهزة",
  "parentId": "uuid",
  "sortOrder": 1
}
```

**Response 201:** Created category object

**Errors:** `422` parentId points to a child category (max depth = 1)

### PUT /admin/categories/{id}

**Request:** `{ name, nameAr, sortOrder }` — all optional.

**Response 200:** Updated category object

### POST /admin/categories/{id}/deactivate / /reactivate

Deactivating a parent also deactivates all children.

---

## TICKET FIELD DEFINITIONS

### GET /admin/field-definitions
**Roles:** `[Admin]`

**Query params:** `departmentId`, `categoryId`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "departmentId": "uuid",
      "departmentName": "Technical Support",
      "categoryId": null,
      "fieldName": "Serial Number",
      "fieldNameAr": "الرقم التسلسلي",
      "fieldType": "Text",
      "options": null,
      "isRequired": false,
      "sortOrder": 1,
      "isActive": true
    }
  ]
}
```

### POST /admin/field-definitions

**Request:**
```json
{
  "departmentId": "uuid",
  "categoryId": null,
  "fieldName": "Serial Number",
  "fieldNameAr": "الرقم التسلسلي",
  "fieldType": "Text",
  "options": null,
  "isRequired": false,
  "sortOrder": 1
}
```

For Dropdown type, `options` is a JSON array of strings:
```json
"options": ["Option A", "Option B", "Option C"]
```

**Response 201:** Created field definition object

### PUT /admin/field-definitions/{id}

**Request:** All POST fields optional.

**Response 200:** Updated field definition

### DELETE /admin/field-definitions/{id}

Soft-deactivate the field. Existing ticket values are retained.

**Response 204:** No content

---

## GLOBAL QUICK-REPLY TEMPLATES

### GET /admin/templates
Lists only Global-scope templates.

**Response 200:** Same shape as `GET /agents/me/templates`

### POST /admin/templates

**Request:**
```json
{
  "title": "Standard Greeting — Arabic",
  "content": "مرحباً {{customer_name}}، شكراً لتواصلك مع {{department}}.",
  "category": "Greeting"
}
```

**Response 201:** Created template with `scope: "Global"`

### PUT /admin/templates/{id} / DELETE /admin/templates/{id}

Same pattern as personal templates but scoped to Global only.

---

## SLA POLICIES

### GET /admin/sla/policies
**Roles:** `[Admin, Manager]`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "priority": "Critical",
      "departmentId": null,
      "firstResponseMinutes": 15,
      "resolutionMinutes": 240,
      "updateFrequencyMinutes": 30,
      "warningThresholdPercent": 80,
      "breachThresholdPercent": 100,
      "criticalBreachThresholdPercent": 200
    }
  ]
}
```

### PUT /admin/sla/policies/{id}
**Roles:** `[Admin]`

**Request:**
```json
{
  "firstResponseMinutes": 10,
  "resolutionMinutes": 180
}
```

**Response 200:** Updated policy object

---

## BUSINESS HOURS

### GET /admin/business-hours
Returns global and all department overrides.

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "isGlobal": true,
      "department": null,
      "workDays": ["Sunday","Monday","Tuesday","Wednesday","Thursday"],
      "startTime": "08:00",
      "endTime": "18:00",
      "timeZone": "Asia/Riyadh",
      "holidays": [
        { "id": "uuid", "date": "2025-09-23", "name": "National Day", "nameAr": "اليوم الوطني" }
      ]
    }
  ]
}
```

### PUT /admin/business-hours/{id}

**Request:**
```json
{
  "workDays": ["Sunday","Monday","Tuesday","Wednesday","Thursday"],
  "startTime": "08:00",
  "endTime": "16:00",
  "timeZone": "Asia/Riyadh"
}
```

**Response 200:** Updated business hours object

### POST /admin/business-hours/{id}/holidays

**Request:**
```json
{ "date": "2025-12-01", "name": "Company Holiday", "nameAr": "إجازة الشركة" }
```

**Response 201:** Created holiday object

### DELETE /admin/business-hours/{id}/holidays/{holidayId}

**Response 204:** No content
