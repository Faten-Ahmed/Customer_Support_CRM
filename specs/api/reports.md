# API Spec — Reports & Management Dashboard

> Base path: `/reports` and `/dashboard`

---

## GET /reports/tickets

Ticket volume report.

**Auth:** Bearer | **Roles:** `[Agent+]`

Agent scope: their departments only. Manager scope: their departments. Admin scope: all.

**Query params:**
| Param | Type | Description |
|-------|------|-------------|
| `dateFrom` | date | Required |
| `dateTo` | date | Required |
| `departmentId` | uuid | Optional filter |
| `agentId` | uuid | Optional filter |
| `groupBy` | string | `day`, `week`, `month` (default: `day`) |

**Response 200:**
```json
{
  "data": {
    "summary": {
      "totalCreated": 320,
      "totalResolved": 298,
      "totalClosed": 275,
      "openAtEndOfPeriod": 22
    },
    "byStatus": {
      "New": 5, "Assigned": 8, "InProgress": 9, "OnHold": 3,
      "Escalated": 2, "Resolved": 23, "Closed": 270
    },
    "byPriority": {
      "Critical": 12, "High": 85, "Medium": 150, "Low": 73
    },
    "byChannel": {
      "Email": 180, "WhatsApp": 60, "SMS": 20, "LiveChat": 45, "Portal": 15
    },
    "trend": [
      { "date": "2025-10-01", "created": 14, "resolved": 12 },
      { "date": "2025-10-02", "created": 18, "resolved": 16 }
    ]
  }
}
```

---

## GET /reports/sla

SLA compliance and breach report.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:** `dateFrom` (required), `dateTo` (required), `departmentId`, `priority`

**Response 200:**
```json
{
  "data": {
    "firstResponseCompliance": {
      "total": 320,
      "met": 295,
      "breached": 25,
      "complianceRate": 92.2
    },
    "resolutionCompliance": {
      "total": 298,
      "met": 270,
      "breached": 28,
      "complianceRate": 90.6
    },
    "byPriority": [
      {
        "priority": "Critical",
        "avgFirstResponseMinutes": 12,
        "avgResolutionMinutes": 210,
        "firstResponseComplianceRate": 95.0,
        "resolutionComplianceRate": 91.7
      }
    ],
    "breachReasons": {
      "Warning": 45, "Breach": 25, "CriticalBreach": 3
    }
  }
}
```

---

## GET /reports/agents

Agent performance report.

**Auth:** Bearer | **Roles:** `[Admin, Manager]`

**Query params:** `dateFrom` (required), `dateTo` (required), `departmentId`, `agentId`

**Response 200:**
```json
{
  "data": [
    {
      "agent": { "id": "uuid", "fullName": "Ahmed Al-Farsi" },
      "department": { "id": "uuid", "name": "Technical Support" },
      "ticketsHandled": 48,
      "ticketsResolved": 44,
      "avgFirstResponseMinutes": 18,
      "avgResolutionMinutes": 320,
      "slaComplianceRate": 93.5,
      "csatScore": 4.6,
      "csatResponseCount": 32,
      "escalationRate": 4.2
    }
  ]
}
```

---

## GET /reports/csat

Customer satisfaction report.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:** `dateFrom` (required), `dateTo` (required), `departmentId`, `agentId`, `categoryId`

**Response 200:**
```json
{
  "data": {
    "overall": {
      "avgRating": 4.4,
      "totalSent": 250,
      "totalSubmitted": 185,
      "responseRate": 74.0
    },
    "distribution": { "1": 5, "2": 8, "3": 20, "4": 62, "5": 90 },
    "byDepartment": [
      { "department": "Technical Support", "avgRating": 4.5, "responseCount": 90 }
    ],
    "byAgent": [
      { "agent": { "id": "uuid", "fullName": "Ahmed Al-Farsi" }, "avgRating": 4.6, "responseCount": 32 }
    ],
    "recentComments": [
      {
        "ticketNumber": "TKT-2025-00030",
        "rating": 5,
        "comment": "Very fast and professional support.",
        "submittedAt": "2025-10-14T14:00:00Z"
      }
    ]
  }
}
```

---

## GET /dashboard/kpis

Live KPI snapshot for the management dashboard. Real-time data.

**Auth:** Bearer | **Roles:** `[Agent+]`

**Query params:** `departmentId` (optional — Admin sees all if omitted)

**Response 200:**
```json
{
  "data": {
    "openTickets": {
      "total": 42,
      "byCritical": 3, "byHigh": 12, "byMedium": 18, "byLow": 9
    },
    "slaBreachRate": 7.8,
    "avgFirstResponseMinutes": 22,
    "avgResolutionMinutes": 385,
    "csatScore": 4.4,
    "agentUtilization": 76.5,
    "ticketsTodayCreated": 18,
    "ticketsTodayResolved": 15,
    "escalationRate": 5.2,
    "unassignedTickets": 6,
    "agentWorkload": [
      { "agent": { "id": "uuid", "fullName": "Ahmed Al-Farsi" }, "openTickets": 9, "status": "Available" }
    ],
    "calculatedAt": "2025-10-15T11:00:00Z"
  }
}
```

---

## GET /reports/export

Export a report as CSV, Excel, or PDF.

**Auth:** Bearer | **Roles:** `[Admin, Manager]`

**Query params:** `reportType` (`tickets`, `sla`, `agents`, `csat`), `format` (`csv`, `xlsx`, `pdf`), `dateFrom`, `dateTo`, `departmentId`

**Response 200:**
```
Content-Type: text/csv  (or application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, application/pdf)
Content-Disposition: attachment; filename="sla-report-2025-10.csv"
<binary file content>
```

---

## SignalR — DashboardHub

Live dashboard receives real-time KPI pushes — no polling needed.

**Hub URL:** `ws://localhost:5000/hubs/dashboard?access_token=<token>`

**Client methods (server → client):**

| Method | Payload | Trigger |
|--------|---------|---------|
| `KpiUpdated` | Full KPI object (same as GET /dashboard/kpis) | TicketCreated, TicketStatusChanged, CsatSubmitted, SlaBreached |
| `AgentWorkloadUpdated` | `[{ agentId, openTickets, status }]` | TicketAssigned, AgentStatusChanged |
