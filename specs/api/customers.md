# API Spec — Customer Management

> Base path: `/customers`

---

## GET /customers

List customers with search and pagination.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `search` | string | Search by name, email, phone, or externalId |
| `isVip` | bool | Filter VIP customers |
| `page` | int | Default 1 |
| `pageSize` | int | Default 20, max 100 |
| `sortBy` | string | `fullName`, `email`, `createdAt` |
| `sortDir` | string | `asc` / `desc` |

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "fullName": "Sara Al-Mansouri",
      "fullNameAr": "سارة المنصوري",
      "email": "sara@example.com",
      "phone": "+966501234567",
      "companyName": "Tech Corp",
      "companyNameAr": "تك كورب",
      "isVip": false,
      "createdAt": "2025-10-01T08:00:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 85, "totalPages": 5 }
}
```

---

## POST /customers

Create a new customer record.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "fullName": "Sara Al-Mansouri",
  "fullNameAr": "سارة المنصوري",
  "email": "sara@example.com",
  "phone": "+966501234567",
  "companyName": "Tech Corp",
  "companyNameAr": "تك كورب",
  "jobTitle": "IT Manager",
  "country": "Saudi Arabia",
  "city": "Riyadh",
  "street": "King Fahd Road",
  "buildingNumber": "12B",
  "externalId": "ERP-10045"
}
```

**Response 201:**
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
    "jobTitle": "IT Manager",
    "country": "Saudi Arabia",
    "city": "Riyadh",
    "street": "King Fahd Road",
    "buildingNumber": "12B",
    "externalId": "ERP-10045",
    "isVip": false,
    "isActive": true,
    "createdAt": "2025-10-15T09:00:00Z"
  }
}
```

**Errors:** `409` email already exists | `409` externalId already exists

---

## GET /customers/{id}

Get a single customer with full profile and contacts.

**Auth:** Bearer | **Roles:** `[Agent+]`

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
    "jobTitle": "IT Manager",
    "country": "Saudi Arabia",
    "city": "Riyadh",
    "street": "King Fahd Road",
    "buildingNumber": "12B",
    "externalId": "ERP-10045",
    "isVip": false,
    "isActive": true,
    "createdAt": "2025-10-01T08:00:00Z",
    "updatedAt": "2025-10-10T11:30:00Z",
    "contacts": [
      {
        "id": "uuid",
        "fullName": "Mohammed Al-Mansouri",
        "email": "m.mansouri@example.com",
        "phone": "+966509876543",
        "contactType": "Billing",
        "isPrimary": false
      }
    ]
  }
}
```

**Errors:** `404` customer not found

---

## PUT /customers/{id}

Update a customer record. All fields optional — only provided fields are updated.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "fullName": "Sara Al-Mansouri",
  "fullNameAr": "سارة المنصوري",
  "phone": "+966501234568",
  "city": "Jeddah"
}
```

**Response 200:** Updated customer object (same shape as GET /customers/{id})

**Errors:** `404` not found | `409` email conflict

---

## DELETE /customers/{id}

Soft-delete a customer. Tickets are retained.

**Auth:** Bearer | **Roles:** `[Admin, Manager]`

**Response 204:** No content

**Errors:** `404` not found

---

## POST /customers/{id}/flag-vip

Toggle the VIP flag on a customer.

**Auth:** Bearer | **Roles:** `[Admin, Manager]`

**Request:**
```json
{ "isVip": true }
```

**Response 200:**
```json
{ "data": { "id": "uuid", "isVip": true } }
```

---

## GET /customers/{id}/tickets

Get all tickets for a customer (paginated).

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:** `status`, `priority`, `page`, `pageSize`

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
      "createdAt": "2025-10-14T07:00:00Z"
    }
  ],
  "meta": { "page": 1, "pageSize": 20, "totalCount": 8, "totalPages": 1 }
}
```

---

## GET /customers/{id}/contacts

List all contacts for a customer.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 200:**
```json
{
  "data": [
    {
      "id": "uuid",
      "fullName": "Mohammed Al-Mansouri",
      "email": "m.mansouri@example.com",
      "phone": "+966509876543",
      "contactType": "Billing",
      "isPrimary": false
    }
  ]
}
```

---

## POST /customers/{id}/contacts

Add a contact to a customer.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:**
```json
{
  "fullName": "Mohammed Al-Mansouri",
  "email": "m.mansouri@example.com",
  "phone": "+966509876543",
  "contactType": "Billing",
  "isPrimary": false
}
```

**Response 201:** Created contact object

---

## PUT /customers/{id}/contacts/{contactId}

Update a customer contact.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Request:** Same fields as POST (all optional)

**Response 200:** Updated contact object

**Errors:** `404` contact not found

---

## DELETE /customers/{id}/contacts/{contactId}

Remove a contact from a customer.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Response 204:** No content

**Errors:** `404` not found | `422` cannot delete the only primary contact
