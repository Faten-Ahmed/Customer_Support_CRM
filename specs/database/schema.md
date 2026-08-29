# Database Schema — AZM Squad Customer Support CRM

> **Status:** Draft — Phase 2
> **Version:** 0.1.0
> **Date:** 2026-08-25
> **Database:** Microsoft SQL Server 2022

---

## Conventions

- All PKs are `UNIQUEIDENTIFIER` (Guid), default `NEWSEQUENTIALID()` for clustered index performance
- All timestamps are `DATETIMEOFFSET(7)` stored in UTC
- `NVARCHAR` used for all text (Unicode — supports Arabic)
- Soft-delete columns: `IsDeleted BIT NOT NULL DEFAULT 0`, `DeletedAt DATETIMEOFFSET NULL`, `DeletedByUserId UNIQUEIDENTIFIER NULL`
- Foreign keys named `FK_<Table>_<ReferencedTable>`
- Indexes named `IX_<Table>_<Columns>`
- All tables in `dbo` schema unless noted

---

## Schema: Identity & Access

### Table: Users
```sql
Users (
  Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  Email                   NVARCHAR(256)       NOT NULL,
  PasswordHash            NVARCHAR(MAX)       NOT NULL,
  FirstName               NVARCHAR(100)       NOT NULL,
  LastName                NVARCHAR(100)       NOT NULL,
  FirstNameAr             NVARCHAR(100)       NULL,
  LastNameAr              NVARCHAR(100)       NULL,
  JobTitle                NVARCHAR(200)       NULL,
  JobTitleAr              NVARCHAR(200)       NULL,
  Role                    TINYINT             NOT NULL,  -- UserRole enum
  PrimaryDepartmentId     UNIQUEIDENTIFIER    NULL,
  AvailabilityStatus      TINYINT             NULL,      -- AgentStatus enum; NULL for Customer role
  LastAvailabilityChange  DATETIMEOFFSET      NULL,
  AutoAwayAfterMinutes    INT                 NOT NULL DEFAULT 10,
  IsActive                BIT                 NOT NULL DEFAULT 1,
  RequiresPasswordChange  BIT                 NOT NULL DEFAULT 0,
  CreatedAt               DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  LastLoginAt             DATETIMEOFFSET      NULL,
  IsDeleted               BIT                 NOT NULL DEFAULT 0,
  DeletedAt               DATETIMEOFFSET      NULL,
  DeletedByUserId         UNIQUEIDENTIFIER    NULL,
  CONSTRAINT PK_Users PRIMARY KEY (Id),
  CONSTRAINT UQ_Users_Email UNIQUE (Email),
  CONSTRAINT FK_Users_Department
    FOREIGN KEY (PrimaryDepartmentId) REFERENCES Departments(Id),
  CONSTRAINT CHK_Users_Role
    CHECK (Role IN (1, 2, 3, 4))
)

Indexes:
  IX_Users_Email          ON Users (Email)
  IX_Users_Role           ON Users (Role)
  IX_Users_PrimaryDept    ON Users (PrimaryDepartmentId)
  IX_Users_IsActive       ON Users (IsActive) WHERE IsDeleted = 0
```

### Table: Departments
```sql
Departments (
  Id               UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  Name             NVARCHAR(200)       NOT NULL,
  NameAr           NVARCHAR(200)       NOT NULL,
  Description      NVARCHAR(1000)      NULL,
  DescriptionAr      NVARCHAR(1000)      NULL,
  BusinessHoursId  UNIQUEIDENTIFIER    NULL,
  IsActive         BIT                 NOT NULL DEFAULT 1,
  CreatedAt        DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Departments PRIMARY KEY (Id),
  CONSTRAINT FK_Departments_BusinessHours
    FOREIGN KEY (BusinessHoursId) REFERENCES BusinessHours(Id)
)

Indexes:
  IX_Departments_IsActive    ON Departments (IsActive)
```

### Table: Branches
```sql
Branches (
  Id        UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  Name      NVARCHAR(200)       NOT NULL,
  NameAr    NVARCHAR(200)       NOT NULL,
  IsActive  BIT                 NOT NULL DEFAULT 1,
  CreatedAt DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Branches PRIMARY KEY (Id)
)
```

### Table: AgentDepartments
```sql
AgentDepartments (
  AgentId       UNIQUEIDENTIFIER    NOT NULL,
  DepartmentId  UNIQUEIDENTIFIER    NOT NULL,
  IsPrimary     BIT                 NOT NULL DEFAULT 0,
  AssignedAt    DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_AgentDepartments PRIMARY KEY (AgentId, DepartmentId),
  CONSTRAINT FK_AgentDepartments_Agent
    FOREIGN KEY (AgentId) REFERENCES Users(Id),
  CONSTRAINT FK_AgentDepartments_Department
    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
)

Indexes:
  IX_AgentDepartments_DepartmentId    ON AgentDepartments (DepartmentId)
  IX_AgentDepartments_IsPrimary       ON AgentDepartments (AgentId) WHERE IsPrimary = 1
```

### Table: AgentSkills
```sql
AgentSkills (
  AgentId     UNIQUEIDENTIFIER    NOT NULL,
  CategoryId  UNIQUEIDENTIFIER    NOT NULL,
  CONSTRAINT PK_AgentSkills PRIMARY KEY (AgentId, CategoryId),
  CONSTRAINT FK_AgentSkills_Agent
    FOREIGN KEY (AgentId) REFERENCES Users(Id),
  CONSTRAINT FK_AgentSkills_Category
    FOREIGN KEY (CategoryId) REFERENCES TicketCategories(Id)
)
```

---

## Schema: Customer

### Table: Customers
```sql
Customers (
  Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  FullName        NVARCHAR(200)       NOT NULL,
  FullNameAr      NVARCHAR(200)       NULL,
  Email           NVARCHAR(256)       NOT NULL,
  Phone           NVARCHAR(50)        NOT NULL,
  CompanyName     NVARCHAR(200)       NULL,
  CompanyNameAr   NVARCHAR(200)       NULL,
  JobTitle        NVARCHAR(200)       NULL,
  Country         NVARCHAR(100)       NULL,
  City            NVARCHAR(100)       NULL,
  Street          NVARCHAR(300)       NULL,
  BuildingNumber  NVARCHAR(50)        NULL,
  ExternalId      NVARCHAR(100)       NULL,
  IsVip           BIT                 NOT NULL DEFAULT 0,
  IsActive        BIT                 NOT NULL DEFAULT 1,
  CreatedAt       DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt       DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  IsDeleted       BIT                 NOT NULL DEFAULT 0,
  DeletedAt       DATETIMEOFFSET      NULL,
  DeletedByUserId UNIQUEIDENTIFIER    NULL,
  CONSTRAINT PK_Customers PRIMARY KEY (Id),
  CONSTRAINT UQ_Customers_Email UNIQUE (Email),
  CONSTRAINT UQ_Customers_ExternalId UNIQUE (ExternalId)
  -- Note: SQL Server unique index allows multiple NULLs by default
)

Indexes:
  IX_Customers_Email         ON Customers (Email)                    -- covered by UQ
  IX_Customers_ExternalId    ON Customers (ExternalId)               -- covered by UQ
  IX_Customers_Phone         ON Customers (Phone)
  IX_Customers_IsVip         ON Customers (IsVip) WHERE IsVip = 1
  IX_Customers_IsDeleted     ON Customers (IsDeleted)
```

### Table: CustomerContacts
```sql
CustomerContacts (
  Id           UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  CustomerId   UNIQUEIDENTIFIER    NOT NULL,
  FullName     NVARCHAR(200)       NOT NULL,
  Email        NVARCHAR(256)       NULL,
  Phone        NVARCHAR(50)        NULL,
  ContactType  TINYINT             NOT NULL,  -- ContactType enum
  IsPrimary    BIT                 NOT NULL DEFAULT 0,
  CreatedAt    DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_CustomerContacts PRIMARY KEY (Id),
  CONSTRAINT FK_CustomerContacts_Customer
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
  CONSTRAINT CHK_CustomerContacts_Type
    CHECK (ContactType IN (1, 2, 3, 4))
)

Indexes:
  IX_CustomerContacts_CustomerId    ON CustomerContacts (CustomerId)
```

---

## Schema: Ticketing

### Table: TicketCategories
```sql
TicketCategories (
  Id        UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  Name      NVARCHAR(200)       NOT NULL,
  NameAr    NVARCHAR(200)       NOT NULL,
  ParentId  UNIQUEIDENTIFIER    NULL,
  IsActive  BIT                 NOT NULL DEFAULT 1,
  SortOrder INT                 NOT NULL DEFAULT 0,
  CONSTRAINT PK_TicketCategories PRIMARY KEY (Id),
  CONSTRAINT FK_TicketCategories_Parent
    FOREIGN KEY (ParentId) REFERENCES TicketCategories(Id),
  CONSTRAINT CHK_TicketCategories_MaxDepth
    CHECK (ParentId IS NULL OR
           (SELECT ParentId FROM TicketCategories p WHERE p.Id = ParentId) IS NULL)
)

Indexes:
  IX_TicketCategories_ParentId    ON TicketCategories (ParentId)
  IX_TicketCategories_IsActive    ON TicketCategories (IsActive)
```

### Table: TicketFieldDefinitions
```sql
TicketFieldDefinitions (
  Id           UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  DepartmentId UNIQUEIDENTIFIER    NOT NULL,
  CategoryId   UNIQUEIDENTIFIER    NULL,
  FieldName    NVARCHAR(200)       NOT NULL,
  FieldNameAr  NVARCHAR(200)       NOT NULL,
  FieldType    TINYINT             NOT NULL,  -- FieldType enum
  Options      NVARCHAR(MAX)       NULL,      -- JSON array for Dropdown
  IsRequired   BIT                 NOT NULL DEFAULT 0,
  SortOrder    INT                 NOT NULL DEFAULT 0,
  IsActive     BIT                 NOT NULL DEFAULT 1,
  CONSTRAINT PK_TicketFieldDefinitions PRIMARY KEY (Id),
  CONSTRAINT FK_TicketFieldDefs_Department
    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
  CONSTRAINT FK_TicketFieldDefs_Category
    FOREIGN KEY (CategoryId) REFERENCES TicketCategories(Id),
  CONSTRAINT CHK_TicketFieldDefs_Type
    CHECK (FieldType IN (1, 2, 3, 4, 5, 6))
)

Indexes:
  IX_TicketFieldDefs_DepartmentId    ON TicketFieldDefinitions (DepartmentId)
```

### Table: Tickets
```sql
Tickets (
  Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  TicketNumber      NVARCHAR(20)        NOT NULL,
  CustomerId        UNIQUEIDENTIFIER    NOT NULL,
  DepartmentId      UNIQUEIDENTIFIER    NOT NULL,
  CategoryId        UNIQUEIDENTIFIER    NOT NULL,
  Priority          TINYINT             NOT NULL,
  Status            TINYINT             NOT NULL DEFAULT 1,  -- New
  Subject           NVARCHAR(500)       NOT NULL,
  SubjectAr         NVARCHAR(500)       NULL,
  Description       NVARCHAR(MAX)       NOT NULL,
  DescriptionAr     NVARCHAR(MAX)       NULL,
  AssignedAgentId   UNIQUEIDENTIFIER    NULL,
  Channel           TINYINT             NOT NULL,
  IsEscalated       BIT                 NOT NULL DEFAULT 0,
  EscalationLevel   TINYINT             NOT NULL DEFAULT 0,
  CustomFieldValues NVARCHAR(MAX)       NULL,     -- JSON object
  CreatedAt         DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt         DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  FirstResponseAt   DATETIMEOFFSET      NULL,
  ResolvedAt        DATETIMEOFFSET      NULL,
  ClosedAt          DATETIMEOFFSET      NULL,
  IsDeleted         BIT                 NOT NULL DEFAULT 0,
  DeletedAt         DATETIMEOFFSET      NULL,
  DeletedByUserId   UNIQUEIDENTIFIER    NULL,
  CONSTRAINT PK_Tickets PRIMARY KEY (Id),
  CONSTRAINT UQ_Tickets_TicketNumber UNIQUE (TicketNumber),
  CONSTRAINT FK_Tickets_Customer
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
  CONSTRAINT FK_Tickets_Department
    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
  CONSTRAINT FK_Tickets_Category
    FOREIGN KEY (CategoryId) REFERENCES TicketCategories(Id),
  CONSTRAINT FK_Tickets_Agent
    FOREIGN KEY (AssignedAgentId) REFERENCES Users(Id),
  CONSTRAINT CHK_Tickets_Priority
    CHECK (Priority IN (1, 2, 3, 4)),
  CONSTRAINT CHK_Tickets_Status
    CHECK (Status IN (1, 2, 3, 4, 5, 6, 7, 8)),
  CONSTRAINT CHK_Tickets_Channel
    CHECK (Channel IN (1, 2, 3, 4, 5, 6))
)

Indexes:
  IX_Tickets_CustomerId        ON Tickets (CustomerId)
  IX_Tickets_DepartmentId      ON Tickets (DepartmentId)
  IX_Tickets_AssignedAgentId   ON Tickets (AssignedAgentId)
  IX_Tickets_Status            ON Tickets (Status)
  IX_Tickets_Priority          ON Tickets (Priority)
  IX_Tickets_CreatedAt         ON Tickets (CreatedAt DESC)
  IX_Tickets_IsDeleted         ON Tickets (IsDeleted)
  IX_Tickets_Dept_Status       ON Tickets (DepartmentId, Status) WHERE IsDeleted = 0
  IX_Tickets_Agent_Status      ON Tickets (AssignedAgentId, Status) WHERE IsDeleted = 0
```

### Table: TicketMessages
```sql
TicketMessages (
  Id               UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  TicketId         UNIQUEIDENTIFIER    NOT NULL,
  SenderId         UNIQUEIDENTIFIER    NULL,
  SenderEmail      NVARCHAR(256)       NULL,
  Content          NVARCHAR(MAX)       NOT NULL,
  ContentAr          NVARCHAR(MAX)       NULL,
  IsInternal       BIT                 NOT NULL DEFAULT 0,
  Channel          TINYINT             NOT NULL,
  MentionedUserIds NVARCHAR(MAX)       NULL,  -- JSON array of GUIDs
  CreatedAt        DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_TicketMessages PRIMARY KEY (Id),
  CONSTRAINT FK_TicketMessages_Ticket
    FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
  CONSTRAINT FK_TicketMessages_Sender
    FOREIGN KEY (SenderId) REFERENCES Users(Id)
)

Indexes:
  IX_TicketMessages_TicketId     ON TicketMessages (TicketId, CreatedAt)
  IX_TicketMessages_SenderId     ON TicketMessages (SenderId)
  IX_TicketMessages_Internal     ON TicketMessages (TicketId, IsInternal)
```

### Table: TicketAttachments
```sql
TicketAttachments (
  Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  TicketId          UNIQUEIDENTIFIER    NOT NULL,
  UploadedByUserId  UNIQUEIDENTIFIER    NOT NULL,
  FileName          NVARCHAR(500)       NOT NULL,
  ContentType       NVARCHAR(200)       NOT NULL,
  FileSizeBytes     BIGINT              NOT NULL,
  StorageKey        NVARCHAR(1000)      NOT NULL,
  UploadedAt        DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  ExpiresAt         DATETIMEOFFSET      NOT NULL,
  IsDeleted         BIT                 NOT NULL DEFAULT 0,
  DeletedAt         DATETIMEOFFSET      NULL,
  DeletedByUserId   UNIQUEIDENTIFIER    NULL,
  CONSTRAINT PK_TicketAttachments PRIMARY KEY (Id),
  CONSTRAINT FK_TicketAttachments_Ticket
    FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
  CONSTRAINT FK_TicketAttachments_Uploader
    FOREIGN KEY (UploadedByUserId) REFERENCES Users(Id)
)

Indexes:
  IX_TicketAttachments_TicketId     ON TicketAttachments (TicketId) WHERE IsDeleted = 0
  IX_TicketAttachments_ExpiresAt    ON TicketAttachments (ExpiresAt) WHERE IsDeleted = 0
```

### Table: TicketStatusHistory
```sql
TicketStatusHistory (
  Id               UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  TicketId         UNIQUEIDENTIFIER    NOT NULL,
  FromStatus       TINYINT             NULL,
  ToStatus         TINYINT             NOT NULL,
  ChangedByUserId  UNIQUEIDENTIFIER    NOT NULL,
  ChangedAt        DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  Reason           NVARCHAR(1000)      NULL,
  CONSTRAINT PK_TicketStatusHistory PRIMARY KEY (Id),
  CONSTRAINT FK_TicketStatusHistory_Ticket
    FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
  CONSTRAINT FK_TicketStatusHistory_User
    FOREIGN KEY (ChangedByUserId) REFERENCES Users(Id)
)

Indexes:
  IX_TicketStatusHistory_TicketId    ON TicketStatusHistory (TicketId, ChangedAt)
```

### Table: TicketDepartmentHistory
```sql
TicketDepartmentHistory (
  Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  TicketId            UNIQUEIDENTIFIER    NOT NULL,
  FromDepartmentId    UNIQUEIDENTIFIER    NOT NULL,
  ToDepartmentId      UNIQUEIDENTIFIER    NOT NULL,
  TransferredByUserId UNIQUEIDENTIFIER    NOT NULL,
  TransferredAt       DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  Reason              NVARCHAR(1000)      NOT NULL,
  CONSTRAINT PK_TicketDepartmentHistory PRIMARY KEY (Id),
  CONSTRAINT FK_TicketDeptHistory_Ticket
    FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
  CONSTRAINT FK_TicketDeptHistory_FromDept
    FOREIGN KEY (FromDepartmentId) REFERENCES Departments(Id),
  CONSTRAINT FK_TicketDeptHistory_ToDept
    FOREIGN KEY (ToDepartmentId) REFERENCES Departments(Id),
  CONSTRAINT FK_TicketDeptHistory_User
    FOREIGN KEY (TransferredByUserId) REFERENCES Users(Id)
)

Indexes:
  IX_TicketDeptHistory_TicketId    ON TicketDepartmentHistory (TicketId, TransferredAt)
```

---

## Schema: SLA & Automation

### Table: BusinessHours
```sql
BusinessHours (
  Id            UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  DepartmentId  UNIQUEIDENTIFIER    NULL,
  IsGlobal      BIT                 NOT NULL DEFAULT 0,
  WorkDays      NVARCHAR(200)       NOT NULL,  -- JSON: ["Sunday","Monday",...]
  StartTime     TIME(0)             NOT NULL DEFAULT '08:00:00',
  EndTime       TIME(0)             NOT NULL DEFAULT '18:00:00',
  TimeZone      NVARCHAR(100)       NOT NULL DEFAULT 'Asia/Riyadh',
  CONSTRAINT PK_BusinessHours PRIMARY KEY (Id),
  CONSTRAINT FK_BusinessHours_Department
    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
  CONSTRAINT UQ_BusinessHours_Global
    UNIQUE (IsGlobal) WHERE IsGlobal = 1  -- Only one global record
)
```

### Table: BusinessHolidays
```sql
BusinessHolidays (
  Id               UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  BusinessHoursId  UNIQUEIDENTIFIER    NOT NULL,
  Date             DATE                NOT NULL,
  Name             NVARCHAR(200)       NOT NULL,
  NameAr           NVARCHAR(200)       NOT NULL,
  CONSTRAINT PK_BusinessHolidays PRIMARY KEY (Id),
  CONSTRAINT FK_BusinessHolidays_BusinessHours
    FOREIGN KEY (BusinessHoursId) REFERENCES BusinessHours(Id),
  CONSTRAINT UQ_BusinessHolidays_DatePerConfig
    UNIQUE (BusinessHoursId, Date)
)

Indexes:
  IX_BusinessHolidays_Date    ON BusinessHolidays (BusinessHoursId, Date)
```

### Table: SlaPolicies
```sql
SlaPolicies (
  Id                            UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  Priority                      TINYINT             NOT NULL,
  DepartmentId                  UNIQUEIDENTIFIER    NULL,
  FirstResponseMinutes          INT                 NOT NULL,
  ResolutionMinutes             INT                 NOT NULL,
  UpdateFrequencyMinutes        INT                 NOT NULL,
  WarningThresholdPercent       INT                 NOT NULL DEFAULT 80,
  BreachThresholdPercent        INT                 NOT NULL DEFAULT 100,
  CriticalBreachThresholdPercent INT                NOT NULL DEFAULT 200,
  IsActive                      BIT                NOT NULL DEFAULT 1,
  CONSTRAINT PK_SlaPolicies PRIMARY KEY (Id),
  CONSTRAINT FK_SlaPolicies_Department
    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
  CONSTRAINT UQ_SlaPolicies_PriorityPerDept
    UNIQUE (Priority, DepartmentId)
)
```

### Table: SlaClocks
```sql
SlaClocks (
  Id                    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  TicketId              UNIQUEIDENTIFIER    NOT NULL,
  SlaPolicyId           UNIQUEIDENTIFIER    NOT NULL,
  StartedAt             DATETIMEOFFSET      NOT NULL,
  FirstResponseDeadline DATETIMEOFFSET      NOT NULL,
  ResolutionDeadline    DATETIMEOFFSET      NOT NULL,
  FirstResponseAt       DATETIMEOFFSET      NULL,
  IsPaused              BIT                 NOT NULL DEFAULT 0,
  PausedAt              DATETIMEOFFSET      NULL,
  TotalPausedMinutes    INT                 NOT NULL DEFAULT 0,
  BreachLevel           TINYINT             NULL,  -- SlaBreachLevel enum
  BreachedAt            DATETIMEOFFSET      NULL,
  CONSTRAINT PK_SlaClocks PRIMARY KEY (Id),
  CONSTRAINT UQ_SlaClocks_TicketId UNIQUE (TicketId),
  CONSTRAINT FK_SlaClocks_Ticket
    FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
  CONSTRAINT FK_SlaClocks_SlaPolicy
    FOREIGN KEY (SlaPolicyId) REFERENCES SlaPolicies(Id)
)

Indexes:
  IX_SlaClocks_ResolutionDeadline    ON SlaClocks (ResolutionDeadline)
    WHERE FirstResponseAt IS NULL OR BreachLevel IS NULL
  IX_SlaClocks_IsPaused              ON SlaClocks (IsPaused)
```

---

## Schema: Communication

### Table: ChatSessions
```sql
ChatSessions (
  Id                    UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  CustomerId            UNIQUEIDENTIFIER    NULL,
  CustomerEmail         NVARCHAR(256)       NULL,
  AssignedAgentId       UNIQUEIDENTIFIER    NULL,
  Status                TINYINT             NOT NULL DEFAULT 1,  -- ChatStatus enum
  StartedAt             DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  LastActivityAt        DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  ConvertedToTicketId   UNIQUEIDENTIFIER    NULL,
  ConvertedAt           DATETIMEOFFSET      NULL,
  CONSTRAINT PK_ChatSessions PRIMARY KEY (Id),
  CONSTRAINT FK_ChatSessions_Customer
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
  CONSTRAINT FK_ChatSessions_Agent
    FOREIGN KEY (AssignedAgentId) REFERENCES Users(Id),
  CONSTRAINT FK_ChatSessions_Ticket
    FOREIGN KEY (ConvertedToTicketId) REFERENCES Tickets(Id)
)

Indexes:
  IX_ChatSessions_Status              ON ChatSessions (Status)
  IX_ChatSessions_LastActivityAt      ON ChatSessions (LastActivityAt)
    WHERE Status IN (1, 2, 3, 4)  -- Active sessions only
```

---

## Schema: Knowledge Base

### Table: KbArticles
```sql
KbArticles (
  Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  Title             NVARCHAR(500)       NOT NULL,
  TitleAr           NVARCHAR(500)       NOT NULL,
  Content           NVARCHAR(MAX)       NOT NULL,
  ContentAr         NVARCHAR(MAX)       NOT NULL,
  Visibility        TINYINT             NOT NULL,   -- ArticleVisibility enum
  Status            TINYINT             NOT NULL DEFAULT 1,  -- ArticleStatus enum; Draft
  AuthorId          UNIQUEIDENTIFIER    NOT NULL,
  ReviewedByUserId  UNIQUEIDENTIFIER    NULL,
  PublishedByUserId UNIQUEIDENTIFIER    NULL,
  CategoryId        UNIQUEIDENTIFIER    NULL,
  Tags              NVARCHAR(MAX)       NULL,   -- JSON array
  ViewCount         INT                 NOT NULL DEFAULT 0,
  CreatedAt         DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt         DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  PublishedAt       DATETIMEOFFSET      NULL,
  ArchivedAt        DATETIMEOFFSET      NULL,
  CONSTRAINT PK_KbArticles PRIMARY KEY (Id),
  CONSTRAINT FK_KbArticles_Author
    FOREIGN KEY (AuthorId) REFERENCES Users(Id),
  CONSTRAINT FK_KbArticles_Reviewer
    FOREIGN KEY (ReviewedByUserId) REFERENCES Users(Id),
  CONSTRAINT FK_KbArticles_Publisher
    FOREIGN KEY (PublishedByUserId) REFERENCES Users(Id),
  CONSTRAINT FK_KbArticles_Category
    FOREIGN KEY (CategoryId) REFERENCES TicketCategories(Id)
)

Indexes:
  IX_KbArticles_Status         ON KbArticles (Status)
  IX_KbArticles_Visibility     ON KbArticles (Visibility, Status)
  IX_KbArticles_CategoryId     ON KbArticles (CategoryId)
  -- Full-text index for search:
  -- CREATE FULLTEXT INDEX ON KbArticles(Title, Content, TitleAr, ContentAr)
  --   KEY INDEX PK_KbArticles ON FT_CRM_Catalog
```

---

## Schema: Notifications

### Table: Notifications
```sql
Notifications (
  Id           UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  RecipientId  UNIQUEIDENTIFIER    NOT NULL,
  Type         SMALLINT            NOT NULL,  -- NotificationType enum
  Title        NVARCHAR(500)       NOT NULL,
  TitleAr        NVARCHAR(500)       NOT NULL,
  Body         NVARCHAR(2000)      NOT NULL,
  BodyAr         NVARCHAR(2000)      NOT NULL,
  EntityType   NVARCHAR(100)       NULL,
  EntityId     UNIQUEIDENTIFIER    NULL,
  IsRead       BIT                 NOT NULL DEFAULT 0,
  ReadAt       DATETIMEOFFSET      NULL,
  CreatedAt    DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_Notifications PRIMARY KEY (Id),
  CONSTRAINT FK_Notifications_Recipient
    FOREIGN KEY (RecipientId) REFERENCES Users(Id)
)

Indexes:
  IX_Notifications_Recipient_Unread    ON Notifications (RecipientId, CreatedAt DESC)
    WHERE IsRead = 0
  IX_Notifications_CreatedAt           ON Notifications (CreatedAt)  -- for cleanup job
```

---

## Schema: Agent Productivity

### Table: AgentTasks
```sql
AgentTasks (
  Id           UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  AgentId      UNIQUEIDENTIFIER    NOT NULL,
  Title        NVARCHAR(500)       NOT NULL,
  TitleAr        NVARCHAR(500)       NOT NULL,
  Description  NVARCHAR(MAX)       NULL,
  DescriptionAr  NVARCHAR(MAX)       NULL,
  Priority     TINYINT             NOT NULL,  -- TaskPriority enum
  Status       TINYINT             NOT NULL DEFAULT 1,  -- TaskStatus enum
  DueAt        DATETIMEOFFSET      NULL,
  TicketId     UNIQUEIDENTIFIER    NULL,
  CustomerId   UNIQUEIDENTIFIER    NULL,
  CompletedAt  DATETIMEOFFSET      NULL,
  CreatedAt    DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_AgentTasks PRIMARY KEY (Id),
  CONSTRAINT FK_AgentTasks_Agent
    FOREIGN KEY (AgentId) REFERENCES Users(Id),
  CONSTRAINT FK_AgentTasks_Ticket
    FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
  CONSTRAINT FK_AgentTasks_Customer
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
)

Indexes:
  IX_AgentTasks_Agent_Status    ON AgentTasks (AgentId, Status)
  IX_AgentTasks_DueAt           ON AgentTasks (DueAt) WHERE Status != 3  -- Not completed
```

### Table: QuickReplyTemplates
```sql
QuickReplyTemplates (
  Id                UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  Title             NVARCHAR(200)       NOT NULL,
  TitleAr             NVARCHAR(200)       NOT NULL,
  Content           NVARCHAR(MAX)       NOT NULL,
  ContentAr           NVARCHAR(MAX)       NOT NULL,
  Category          NVARCHAR(100)       NOT NULL,
  Scope             TINYINT             NOT NULL,  -- TemplateScope enum
  CreatedByUserId   UNIQUEIDENTIFIER    NOT NULL,
  IsActive          BIT                 NOT NULL DEFAULT 1,
  CreatedAt         DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_QuickReplyTemplates PRIMARY KEY (Id),
  CONSTRAINT FK_QuickReplyTemplates_User
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
)

Indexes:
  IX_QRT_Scope_Creator    ON QuickReplyTemplates (Scope, CreatedByUserId)
    WHERE IsActive = 1
```

---

## Schema: Reporting

### Table: CsatSurveys
```sql
CsatSurveys (
  Id           UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  TicketId     UNIQUEIDENTIFIER    NOT NULL,
  CustomerId   UNIQUEIDENTIFIER    NOT NULL,
  AgentId      UNIQUEIDENTIFIER    NOT NULL,
  DepartmentId UNIQUEIDENTIFIER    NOT NULL,
  SentAt       DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  SubmittedAt  DATETIMEOFFSET      NULL,
  Rating       TINYINT             NULL,  -- 1–5
  Comment      NVARCHAR(2000)      NULL,
  CommentAr      NVARCHAR(2000)      NULL,
  IsExpired    BIT                 NOT NULL DEFAULT 0,
  CONSTRAINT PK_CsatSurveys PRIMARY KEY (Id),
  CONSTRAINT UQ_CsatSurveys_TicketId UNIQUE (TicketId),
  CONSTRAINT FK_CsatSurveys_Ticket
    FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
  CONSTRAINT FK_CsatSurveys_Customer
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
  CONSTRAINT FK_CsatSurveys_Agent
    FOREIGN KEY (AgentId) REFERENCES Users(Id),
  CONSTRAINT FK_CsatSurveys_Department
    FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
  CONSTRAINT CHK_CsatSurveys_Rating
    CHECK (Rating IS NULL OR Rating BETWEEN 1 AND 5)
)

Indexes:
  IX_CsatSurveys_AgentId        ON CsatSurveys (AgentId, SubmittedAt)
  IX_CsatSurveys_DepartmentId   ON CsatSurveys (DepartmentId, SubmittedAt)
```

---

## Schema: Audit

### Table: AuditLogs
```sql
AuditLogs (
  Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
  UserId      UNIQUEIDENTIFIER    NULL,
  UserName    NVARCHAR(200)       NOT NULL,
  Action      NVARCHAR(100)       NOT NULL,
  EntityType  NVARCHAR(100)       NOT NULL,
  EntityId    NVARCHAR(100)       NULL,
  OldValues   NVARCHAR(MAX)       NULL,  -- JSON
  NewValues   NVARCHAR(MAX)       NULL,  -- JSON
  IpAddress   NVARCHAR(50)        NULL,
  UserAgent   NVARCHAR(500)       NULL,
  Reason      NVARCHAR(1000)      NULL,
  OccurredAt  DATETIMEOFFSET      NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_AuditLogs PRIMARY KEY (Id)
  -- No FK on UserId — user may be deleted; name is snapshot
)

Indexes:
  IX_AuditLogs_UserId        ON AuditLogs (UserId, OccurredAt DESC)
  IX_AuditLogs_EntityType    ON AuditLogs (EntityType, EntityId, OccurredAt DESC)
  IX_AuditLogs_OccurredAt    ON AuditLogs (OccurredAt)  -- for retention cleanup
```

---

## Table Summary

| Table | Context | Rows (est. 1yr) |
|-------|---------|-----------------|
| Users | Identity | Hundreds |
| Departments | Identity | Tens |
| Branches | Identity | Tens |
| AgentDepartments | Identity | Hundreds |
| AgentSkills | Identity | Hundreds |
| Customers | Customer | Tens of thousands |
| CustomerContacts | Customer | Tens of thousands |
| TicketCategories | Ticketing | ~50 (admin-managed) |
| TicketFieldDefinitions | Ticketing | ~100 |
| Tickets | Ticketing | Hundreds of thousands |
| TicketMessages | Ticketing | Millions |
| TicketAttachments | Ticketing | Hundreds of thousands |
| TicketStatusHistory | Ticketing | Millions |
| TicketDepartmentHistory | Ticketing | Thousands |
| BusinessHours | SLA | Tens |
| BusinessHolidays | SLA | Hundreds |
| SlaPolicies | SLA | ~20 (4 priorities × global + depts) |
| SlaClocks | SLA | One per open ticket |
| ChatSessions | Communication | Thousands |
| KbArticles | KnowledgeBase | Hundreds |
| Notifications | Notifications | Millions (cleaned after 90 days) |
| AgentTasks | Productivity | Thousands |
| QuickReplyTemplates | Productivity | Hundreds |
| CsatSurveys | Reporting | Hundreds of thousands |
| AuditLogs | Audit | Millions (retained 2 years) |
