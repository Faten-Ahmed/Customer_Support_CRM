# Domain Model — AZM Squad Customer Support CRM

> **Status:** Draft — Phase 2
> **Version:** 0.1.0
> **Date:** 2026-08-25

---

## Conventions

- All IDs are `Guid` (UUID v4)
- All timestamps are `DateTimeOffset` (UTC stored, displayed in KSA UTC+3)
- `?` suffix denotes nullable
- Soft-delete: `IsDeleted + DeletedAt + DeletedByUserId` pattern applied to Customer, Ticket, User
- Arabic/English: bilingual entities carry both `Name` and `NameAr` fields
- JSON columns store flexible data (custom field values, mentions arrays)

---

## Context 1 — Identity & Access

### Enum: UserRole
```
Admin    = 1   // Full system control
Manager  = 2   // Department-level full access
Agent    = 3   // Assigned tickets + department tickets
Customer = 4   // Own tickets only (portal)
```

### Enum: AgentStatus
```
Available = 1
Busy      = 2
Away      = 3
Offline   = 4
```

### Aggregate Root: User
```
User
├── Id:                    Guid            (PK)
├── FirstName:             string          (required)
├── LastName:              string          (required)
├── FirstNameAr:           string?         (Arabic first name)
├── LastNameAr:            string?         (Arabic last name)
├── JobTitle:              string?
├── JobTitleAr:            string?
├── Email:                 string          (unique, required)
├── Role:                  UserRole        (required)
├── PrimaryDepartmentId:   Guid?           (FK → Department; null for Customer role)
├── AvailabilityStatus:    AgentStatus?    (only relevant for Agent/Manager roles)
├── LastAvailabilityChange: DateTimeOffset?
├── AutoAwayAfterMinutes:  int             (default: 10)
├── IsActive:              bool            (default: true)
├── RequiresPasswordChange: bool           (default: false; true on first login)
├── CreatedAt:             DateTimeOffset
├── LastLoginAt:           DateTimeOffset?
├── IsDeleted:             bool
├── DeletedAt:             DateTimeOffset?
└── DeletedByUserId:       Guid?

Business Rules:
  - Customer role: PrimaryDepartmentId must be null
  - Agent/Manager role: PrimaryDepartmentId must be set
  - Deactivating a user does not delete their history
  - Auto-away triggers after AutoAwayAfterMinutes of inactivity
  - Tickets are not auto-assigned to Offline agents
  - RequiresPasswordChange is set to true on first-login temp password; cleared after user sets their own password
  - FullName (computed) = FirstName + " " + LastName
```

### Entity: Department
```
Department
├── Id:              Guid           (PK)
├── Name:            string         (required)
├── NameAr:          string         (required)
├── Description:     string?
├── BusinessHoursId: Guid?          (FK → BusinessHours; null = use global)
├── IsActive:        bool
└── CreatedAt:       DateTimeOffset
```

### Entity: Branch
```
Branch
├── Id:        Guid           (PK)
├── Name:      string         (required)
├── NameAr:    string         (required)
├── IsActive:  bool
└── CreatedAt: DateTimeOffset
```

### Entity: AgentDepartment (join)
```
AgentDepartment
├── AgentId:       Guid            (FK → User; composite PK)
├── DepartmentId:  Guid            (FK → Department; composite PK)
├── IsPrimary:     bool
└── AssignedAt:    DateTimeOffset

Business Rules:
  - Each agent has exactly one IsPrimary = true row
  - Changing primary department creates a new row and sets old one to IsPrimary = false
  - Agent dashboard shows tickets from ALL assigned departments
```

### Entity: AgentSkill (join)
```
AgentSkill
├── AgentId:    Guid   (FK → User; composite PK)
└── CategoryId: Guid   (FK → TicketCategory; composite PK)

Business Rules:
  - Skills map an agent to ticket categories they are eligible to handle
  - Used by auto-assignment (skills-based routing)
  - CategoryId can reference either parent or child categories
```

---

## Context 2 — Customer

### Enum: ContactType
```
Primary   = 1
Billing   = 2
Technical = 3
Other     = 4
```

### Aggregate Root: Customer
```
Customer
├── Id:             Guid           (PK)
├── FullName:       string         (required)
├── FullNameAr:     string?        (Arabic full name)
├── Email:          string         (required, unique system-wide)
├── Phone:          string         (required)
├── CompanyName:    string?
├── CompanyNameAr:  string?        (Arabic company name)
├── JobTitle:       string?
├── Country:        string?        (country name or ISO code)
├── City:           string?
├── Street:         string?
├── BuildingNumber: string?
├── ExternalId:     string?        (ERP reference — unique if set)
├── IsVip:          bool           (default: false — triggers escalation if true)
├── IsActive:       bool
├── CreatedAt:      DateTimeOffset
├── UpdatedAt:      DateTimeOffset
├── IsDeleted:      bool
├── DeletedAt:      DateTimeOffset?
└── DeletedByUserId: Guid?

Business Rules:
  - Email uniqueness enforced at DB level (unique index)
  - ExternalId uniqueness enforced at DB level (unique index, nullable — nulls not counted)
  - VIP flag triggers escalation rules (OQ-007)
  - Deleting a customer soft-deletes only — tickets are retained
```

### Entity: CustomerContact
```
CustomerContact
├── Id:          Guid           (PK)
├── CustomerId:  Guid           (FK → Customer)
├── FullName:    string         (required)
├── Email:       string?
├── Phone:       string?
├── ContactType: ContactType    (required)
├── IsPrimary:   bool           (default: false)
└── CreatedAt:   DateTimeOffset

Business Rules:
  - A customer can have multiple contacts
  - At most one contact per customer can have IsPrimary = true
  - ContactType = Primary and IsPrimary = true are independent concepts
```

---

## Context 3 — Ticketing

### Enum: TicketPriority
```
Critical = 1   // P1 — Red
High     = 2   // P2 — Orange
Medium   = 3   // P3 — Yellow
Low      = 4   // P4 — Green
```

### Enum: TicketStatus
```
New        = 1
Assigned   = 2
InProgress = 3
OnHold     = 4   // Waiting for customer response
Escalated  = 5
Resolved   = 6
Reopened   = 7
Closed     = 8
```

### Enum: Channel
```
Email    = 1
WhatsApp = 2
SMS      = 3
LiveChat = 4
WebForm  = 5
Portal   = 6
```

### Aggregate Root: Ticket
```
Ticket
├── Id:                  Guid            (PK)
├── TicketNumber:        string          (unique; format: TKT-YYYY-NNNNN)
├── CustomerId:          Guid            (FK → Customer)
├── DepartmentId:        Guid            (FK → Department — current department)
├── CategoryId:          Guid            (FK → TicketCategory — must be a child category)
├── Priority:            TicketPriority
├── Status:              TicketStatus    (default: New)
├── Subject:             string          (required)
├── SubjectAr:           string?         (Arabic subject)
├── Description:         string          (required)
├── DescriptionAr:       string?         (Arabic description)
├── AssignedAgentId:     Guid?           (FK → User — must be Agent or Manager in the department)
├── Channel:             Channel         (channel through which ticket was created)
├── IsEscalated:         bool            (default: false)
├── EscalationLevel:     int             (0 = none, 1 = Manager, 2 = Admin)
├── CustomFieldValues:   string          (JSON object — keys are TicketFieldDefinition.Id)
├── CreatedAt:           DateTimeOffset
├── UpdatedAt:           DateTimeOffset
├── FirstResponseAt:     DateTimeOffset? (set when first agent message sent to customer)
├── ResolvedAt:          DateTimeOffset?
├── ClosedAt:            DateTimeOffset?
├── IsDeleted:           bool
├── DeletedAt:           DateTimeOffset?
└── DeletedByUserId:     Guid?

Business Rules:
  - TicketNumber auto-generated on creation (sequence per year, padded to 5 digits)
  - CategoryId must reference a child category (ParentId is not null)
  - AssignedAgentId must belong to the ticket's DepartmentId
  - Status transitions follow the allowed matrix (see OQ-006)
  - Priority auto-raises by 1 level on each escalation (P4→P3→P2→P1→Admin alert)
  - P1 (Critical) cannot be raised further — triggers Admin notification instead
  - OnHold status pauses SLA clock via SLA context
  - Escalation by non-Manager/Admin is not permitted
```

### Status Transition Rules
```
Allowed transitions (FromStatus → ToStatus → AllowedRoles):
  New        → Assigned    → [Manager, Admin]
  New        → Closed      → [Admin]
  Assigned   → InProgress  → [Agent (self-assigned)]
  Assigned   → Resolved    → [Agent, Manager]
  InProgress → Resolved    → [Agent, Manager]
  InProgress → Escalated   → [Agent, Manager]
  InProgress → OnHold      → [Agent, Manager]
  OnHold     → InProgress  → [Agent, Manager, System]
  Escalated  → InProgress  → [Manager, Admin]
  Resolved   → Closed      → [Agent, Manager, Customer]
  Resolved   → Reopened    → [Agent, Manager, Customer]
  Closed     → Reopened    → [Agent, Manager, Admin]
  Reopened   → InProgress  → [System (auto after assignment)]
```

### Entity: TicketMessage
```
TicketMessage
├── Id:               Guid            (PK)
├── TicketId:         Guid            (FK → Ticket)
├── SenderId:         Guid?           (FK → User; null if sent by anonymous/external)
├── SenderEmail:      string?         (for external/channel-originated messages)
├── Content:          string          (message body — plain text or HTML)
├── IsInternal:       bool            (true = internal note; false = customer-facing)
├── Channel:          Channel
├── MentionedUserIds: string          (JSON array of Guid — @mentioned users)
└── CreatedAt:        DateTimeOffset

Business Rules:
  - Customers on the portal can only see IsInternal = false messages
  - @mentions trigger Notification for each mentioned user
  - Internal notes do not trigger outbound channel messages
```

### Entity: TicketAttachment
```
TicketAttachment
├── Id:               Guid            (PK)
├── TicketId:         Guid            (FK → Ticket)
├── UploadedByUserId: Guid            (FK → User)
├── FileName:         string
├── ContentType:      string          (MIME type)
├── FileSizeBytes:    long
├── StorageKey:       string          (S3 object key)
├── UploadedAt:       DateTimeOffset
├── ExpiresAt:        DateTimeOffset  (UploadedAt + 2 years)
├── IsDeleted:        bool
├── DeletedAt:        DateTimeOffset?
└── DeletedByUserId:  Guid?

Business Rules:
  - Allowed MIME types: PDF, JPG/JPEG, PNG, DOCX, XLSX, TXT
  - Max file size: 5 MB
  - Max total per ticket: 10 MB (sum of non-deleted attachments)
  - Max total per customer (all tickets): 50 MB
  - Auto-delete job runs daily; deletes files where ExpiresAt < now
  - Deletion always logged in AuditLog
```

### Entity: TicketStatusHistory
```
TicketStatusHistory
├── Id:              Guid             (PK)
├── TicketId:        Guid             (FK → Ticket)
├── FromStatus:      TicketStatus?    (null for initial New status)
├── ToStatus:        TicketStatus
├── ChangedByUserId: Guid             (FK → User)
├── ChangedAt:       DateTimeOffset
└── Reason:          string?

Business Rules:
  - Immutable — never updated or deleted
  - Created on every status transition including initial creation
```

### Entity: TicketDepartmentHistory
```
TicketDepartmentHistory
├── Id:                  Guid           (PK)
├── TicketId:            Guid           (FK → Ticket)
├── FromDepartmentId:    Guid           (FK → Department)
├── ToDepartmentId:      Guid           (FK → Department)
├── TransferredByUserId: Guid           (FK → User)
├── TransferredAt:       DateTimeOffset
└── Reason:              string         (required — mandatory for all transfers)

Business Rules:
  - Immutable — never updated or deleted
  - Only Manager or Admin can transfer between departments
  - Reason field is mandatory
```

### Aggregate Root: TicketCategory
```
TicketCategory
├── Id:        Guid    (PK)
├── Name:      string  (required)
├── NameAr:    string  (required)
├── ParentId:  Guid?   (FK → TicketCategory; null = parent category)
├── IsActive:  bool
└── SortOrder: int

Business Rules:
  - Max depth = 1: if ParentId is set, that parent must have ParentId = null
  - Tickets must reference a child category (ParentId is not null)
  - Deactivating a parent deactivates all its children
  - Admin-managed; pre-seeded at launch
```

### Aggregate Root: TicketFieldDefinition
```
TicketFieldDefinition
├── Id:           Guid       (PK)
├── DepartmentId: Guid       (FK → Department)
├── CategoryId:   Guid?      (FK → TicketCategory; null = applies to all categories in dept)
├── FieldName:    string     (required)
├── FieldNameAr:  string     (required)
├── FieldType:    FieldType  (enum)
├── Options:      string?    (JSON array of strings — only for Dropdown type)
├── IsRequired:   bool
├── SortOrder:    int
└── IsActive:     bool

Business Rules:
  - Values stored as JSON on Ticket.CustomFieldValues keyed by this entity's Id
  - Required fields must be validated on ticket creation/update
```

### Enum: FieldType
```
Text     = 1
Number   = 2
Dropdown = 3
Date     = 4
Checkbox = 5
Textarea = 6
```

---

## Context 4 — SLA & Automation

### Enum: SlaBreachLevel
```
Warning        = 1   // 80% of SLA time elapsed
Breach         = 2   // 100% elapsed
CriticalBreach = 3   // 200% elapsed
```

### Aggregate Root: SlaPolicy
```
SlaPolicy
├── Id:                          Guid            (PK)
├── Priority:                    TicketPriority  (required)
├── DepartmentId:                Guid?           (null = global policy)
├── FirstResponseMinutes:        int
├── ResolutionMinutes:           int
├── UpdateFrequencyMinutes:      int
├── WarningThresholdPercent:     int             (default: 80)
├── BreachThresholdPercent:      int             (default: 100)
├── CriticalBreachThresholdPercent: int          (default: 200)
└── IsActive:                    bool

Default Values (from OQ-016):
  Priority     | FirstResponse | Resolution | UpdateFreq
  Critical(P1) | 15 min        | 240 min    | 30 min
  High(P2)     | 30 min        | 1440 min   | 120 min
  Medium(P3)   | 120 min       | 2880 min   | 1440 min
  Low(P4)      | 240 min       | 7200 min*  | 2880 min
  *5 business days = approx 2400 min of business time
```

### Aggregate Root: BusinessHours
```
BusinessHours
├── Id:           Guid    (PK)
├── DepartmentId: Guid?   (null = global default)
├── IsGlobal:     bool
├── WorkDays:     string  (JSON array: ["Sunday","Monday","Tuesday","Wednesday","Thursday"])
├── StartTime:    TimeOnly (08:00 default)
├── EndTime:      TimeOnly (18:00 default)
└── TimeZone:     string  (IANA tz string: "Asia/Riyadh" default)

Global Default: Sunday–Thursday 08:00–18:00 Asia/Riyadh
```

### Entity: BusinessHoliday
```
BusinessHoliday
├── Id:               Guid    (PK)
├── BusinessHoursId:  Guid    (FK → BusinessHours)
├── Date:             DateOnly
├── Name:             string
└── NameAr:           string
```

### Aggregate Root: SlaClock
```
SlaClock
├── Id:                    Guid             (PK)
├── TicketId:              Guid             (FK → Ticket; unique)
├── SlaPolicyId:           Guid             (FK → SlaPolicy)
├── StartedAt:             DateTimeOffset
├── FirstResponseDeadline: DateTimeOffset
├── ResolutionDeadline:    DateTimeOffset
├── FirstResponseAt:       DateTimeOffset?  (actual — set when ticket gets first response)
├── IsPaused:              bool             (true when ticket is OnHold)
├── PausedAt:              DateTimeOffset?
├── TotalPausedMinutes:    int              (accumulated pause time)
├── BreachLevel:           SlaBreachLevel?
└── BreachedAt:            DateTimeOffset?

Business Rules:
  - Created automatically when a ticket is created (reacts to TicketCreated event)
  - Deadlines calculated using BusinessHours for the ticket's department
  - Clock pauses when ticket moves to OnHold; resumes when back to InProgress
  - Paused time is excluded from SLA calculation
  - Hangfire job checks all active clocks every minute
```

### Value Object: EscalationTrigger (config-driven)
```
Escalation is triggered by any of:
  1. SLA breach (SlaBreached event)
  2. Customer.IsVip = true AND no response within FirstResponseDeadline
  3. Keyword match in ticket subject/description: "urgent", "critical", "unacceptable"
  4. Repeated contacts: 3+ customer messages with no agent response
  5. Manual escalation by Manager or Admin
  6. Customer.IsHighValue = true (spending threshold — configurable in admin)
  7. Ticket tagged as security/privacy sensitive

Each trigger is configurable (on/off) in Admin → SLA Settings
```

---

## Context 5 — Communication

### Aggregate Root: ChatSession
```
ChatSession
├── Id:              Guid            (PK)
├── CustomerId:      Guid?           (FK → Customer; null if pre-login chat)
├── CustomerEmail:   string?         (captured during anonymous chat)
├── AssignedAgentId: Guid?           (FK → User)
├── Status:          ChatStatus      (enum)
├── StartedAt:       DateTimeOffset
├── LastActivityAt:  DateTimeOffset
├── ConvertedToTicketId: Guid?       (FK → Ticket — set when chat → ticket)
└── ConvertedAt:     DateTimeOffset?

Business Rules:
  - AI bot handles first message; escalates to agent on trigger conditions
  - Auto-converts to ticket after 5 minutes of inactivity
  - Chat transcript is included in the resulting Ticket.Description
```

### Enum: ChatStatus
```
Active    = 1
WithBot   = 2
WithAgent = 3
Inactive  = 4
Converted = 5   // Converted to ticket
Closed    = 6
```

---

## Context 6 — Knowledge Base

### Enum: ArticleVisibility
```
Internal = 1   // Agents/Managers/Admins only
Public   = 2   // Customer portal
Both     = 3   // Visible to all
```

### Enum: ArticleStatus
```
Draft     = 1
Review    = 2
Published = 3
Archived  = 4
```

### Aggregate Root: KbArticle
```
KbArticle
├── Id:               Guid              (PK)
├── Title:            string            (required)
├── TitleAr:          string            (required)
├── Content:          string            (rich text / markdown)
├── ContentAr:        string
├── Visibility:       ArticleVisibility
├── Status:           ArticleStatus     (default: Draft)
├── AuthorId:         Guid              (FK → User)
├── ReviewedByUserId: Guid?             (FK → User)
├── PublishedByUserId: Guid?            (FK → User)
├── CategoryId:       Guid?             (FK → TicketCategory — for AI suggestion matching)
├── Tags:             string            (JSON array of strings)
├── ViewCount:        int               (incremented on each view)
├── CreatedAt:        DateTimeOffset
├── UpdatedAt:        DateTimeOffset
├── PublishedAt:      DateTimeOffset?
└── ArchivedAt:       DateTimeOffset?

Business Rules:
  - Any agent can create a Draft
  - Only Manager, Admin, or designated Editor agents can publish
  - Archived articles are not returned in search results
  - CategoryId link enables AI to suggest relevant articles for a ticket's category
```

---

## Context 7 — Notifications

### Enum: NotificationType
```
TicketAssigned        = 1
TicketStatusChanged   = 2
SlaWarning            = 3
SlaBreached           = 4
SlaCriticalBreach     = 5
Mention               = 6
TaskReminder          = 7
DepartmentTransfer    = 8
ChatHandoffRequested  = 9
ArticleReviewRequired = 10
```

### Aggregate Root: Notification
```
Notification
├── Id:              Guid              (PK)
├── RecipientId:     Guid              (FK → User)
├── Type:            NotificationType
├── Title:           string
├── TitleAr:         string
├── Body:            string
├── BodyAr:          string
├── EntityType:      string?           (e.g., "Ticket", "Task", "Article")
├── EntityId:        Guid?
├── IsRead:          bool              (default: false)
├── ReadAt:          DateTimeOffset?
└── CreatedAt:       DateTimeOffset

Business Rules:
  - Delivered to client via SignalR NotificationHub
  - Hangfire job cleans up read notifications older than 90 days
```

---

## Context 8 — Agent Productivity

### Enum: TaskPriority
```
High   = 1
Medium = 2
Low    = 3
```

### Enum: TaskStatus
```
Pending    = 1
InProgress = 2
Completed  = 3
```

### Enum: TemplateScope
```
Personal = 1   // Created by agent; visible only to that agent
Global   = 2   // Created by Admin; visible to all agents
```

### Aggregate Root: AgentTask
```
AgentTask
├── Id:           Guid          (PK)
├── AgentId:      Guid          (FK → User — must be Agent or Manager)
├── Title:        string        (required)
├── TitleAr:      string?
├── Description:  string?
├── DescriptionAr: string?
├── Priority:     TaskPriority
├── Status:       TaskStatus    (default: Pending)
├── DueAt:        DateTimeOffset?
├── TicketId:     Guid?         (FK → Ticket — optional link)
├── CustomerId:   Guid?         (FK → Customer — optional link)
├── CompletedAt:  DateTimeOffset?
└── CreatedAt:    DateTimeOffset

Business Rules:
  - Personal to the creating agent — not shareable
  - Hangfire sends in-app reminder 15 minutes before DueAt
  - Overdue tasks appear highlighted in the agent dashboard
```

### Aggregate Root: QuickReplyTemplate
```
QuickReplyTemplate
├── Id:               Guid           (PK)
├── Title:            string         (required)
├── TitleAr:          string         (required)
├── Content:          string         (supports {{placeholder}} syntax)
├── ContentAr:        string         (Arabic version)
├── Category:         string         (e.g., Greeting, Billing, Support, Closing)
├── Scope:            TemplateScope
├── CreatedByUserId:  Guid           (FK → User)
├── IsActive:         bool
└── CreatedAt:        DateTimeOffset

Supported Placeholders:
  {{customer_name}}, {{ticket_id}}, {{ticket_subject}},
  {{agent_name}}, {{department}}, {{ticket_status}}, {{ticket_priority}}

Business Rules:
  - Agent: can only create/edit/delete their own Personal templates
  - Admin: can create/edit/delete Global templates
  - Manager: can create Personal templates; cannot create Global
```

---

## Context 9 — Reporting (Read Models)

Read models are populated by consuming domain events. They are denormalized for query performance — not normalized relational entities.

### Read Model: TicketMetricsSnapshot
Updated by: TicketCreated, TicketStatusChanged, TicketClosed

### Read Model: SlaPerformanceReport
Updated by: SlaWarningTriggered, SlaBreached, SlaCriticalBreached, TicketFirstResponseRecorded

### Read Model: AgentPerformanceReport
Updated by: TicketAssigned, TicketResolved, CsatSurveySubmitted

### Read Model: CsatReport
Updated by: CsatSurveySubmitted

### Aggregate Root: CsatSurvey
```
CsatSurvey
├── Id:           Guid            (PK)
├── TicketId:     Guid            (FK → Ticket; unique)
├── CustomerId:   Guid            (FK → Customer)
├── AgentId:      Guid            (FK → User — assigned agent at time of close)
├── DepartmentId: Guid            (FK → Department)
├── SentAt:       DateTimeOffset
├── SubmittedAt:  DateTimeOffset?
├── Rating:       int?            (1–5; null until submitted)
├── Comment:      string?
├── CommentAr:    string?
└── IsExpired:    bool            (true after 7 days with no response)

Business Rules:
  - Auto-created when ticket moves to Closed
  - Email sent to customer AND in-app survey shown in portal
  - Expires 7 days after SentAt if not submitted
```

---

## Cross-Cutting Concerns

### AuditLog (shared infrastructure)
```
AuditLog
├── Id:          Guid            (PK)
├── UserId:      Guid?           (FK → User; null for system actions)
├── UserName:    string          (snapshot — not FK, user may be deleted)
├── Action:      string          (Create, Update, Delete, Login, Logout, Transfer, Escalate…)
├── EntityType:  string          (Customer, Ticket, User, Article, SlaPolicy…)
├── EntityId:    string?
├── OldValues:   string?         (JSON snapshot of previous state)
├── NewValues:   string?         (JSON snapshot of new state)
├── IpAddress:   string?
├── UserAgent:   string?
├── Reason:      string?
└── OccurredAt:  DateTimeOffset

Retention: 2 years; exportable as CSV/Excel/PDF
```

---

## Domain Events Summary

| Event | Published By | Consumed By |
|-------|-------------|-------------|
| UserCreated | Identity | — |
| UserDeactivated | Identity | Ticketing (reassign tickets) |
| AgentStatusChanged | Identity | SLA (routing eligibility) |
| CustomerCreated | Customer | Integration (ERP sync) |
| CustomerVipFlagged | Customer | SLA (escalation trigger) |
| TicketCreated | Ticketing | SLA (start clock), Notifications |
| TicketAssigned | Ticketing | Notifications, SLA |
| TicketStatusChanged | Ticketing | SLA (pause/resume clock), Notifications, Reporting |
| TicketFirstResponseRecorded | Ticketing | SLA (stop first-response clock) |
| TicketEscalated | Ticketing | Notifications, Reporting |
| TicketDepartmentTransferred | Ticketing | Notifications |
| TicketResolved | Ticketing | CsatSurvey (create), SLA (stop clock), Reporting |
| TicketClosed | Ticketing | Reporting |
| TicketReopened | Ticketing | SLA (restart clocks) |
| TicketMessageAdded | Ticketing | Notifications (@mentions) |
| SlaWarningTriggered | SLA | Notifications |
| SlaBreached | SLA | Ticketing (escalate), Notifications |
| SlaCriticalBreached | SLA | Ticketing (escalate to Admin), Notifications |
| TicketAutoAssigned | SLA | Ticketing |
| InboundMessageReceived | Communication | Ticketing (create/update ticket) |
| ChatSessionInactive | Communication | Ticketing (create ticket from transcript) |
| ArticlePublished | KnowledgeBase | AI (index for suggestions) |
| TaskDue | AgentProductivity | Notifications |
| CsatSurveySubmitted | Reporting | Reporting (update CSAT scores) |
| ErpCustomerSynced | Integration | Customer (upsert) |
