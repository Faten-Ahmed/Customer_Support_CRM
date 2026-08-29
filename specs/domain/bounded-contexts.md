# Bounded Contexts — AZM Squad Customer Support CRM

> **Status:** Draft — Phase 2
> **Version:** 0.1.0
> **Date:** 2026-08-25

---

## Overview

The system is decomposed into **10 bounded contexts** following DDD principles. Each context owns its data, enforces its invariants, and communicates with other contexts through domain events — not direct object references.

```
┌─────────────────────────────────────────────────────────────────┐
│                    CORE DOMAIN                                   │
│                                                                 │
│   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │
│   │   CUSTOMER   │───▶│  TICKETING   │◀───│     SLA &    │     │
│   │   CONTEXT    │    │   CONTEXT    │    │  AUTOMATION  │     │
│   └──────────────┘    └──────┬───────┘    └──────────────┘     │
│                              │                                   │
│              ┌───────────────┼───────────────┐                  │
│              ▼               ▼               ▼                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
│   │COMMUNICATION │  │  KNOWLEDGE   │  │NOTIFICATIONS │        │
│   │   CONTEXT    │  │    BASE      │  │   CONTEXT    │        │
│   └──────────────┘  └──────────────┘  └──────────────┘        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                  SUPPORTING DOMAIN                               │
│                                                                 │
│   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │
│   │  IDENTITY &  │    │    AGENT     │    │   REPORTING  │     │
│   │   ACCESS     │    │ PRODUCTIVITY │    │   CONTEXT    │     │
│   └──────────────┘    └──────────────┘    └──────────────┘     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                   GENERIC SUBDOMAIN                              │
│                                                                 │
│   ┌──────────────┐    ┌──────────────┐                         │
│   │      AI      │    │ INTEGRATION  │                         │
│   │   CONTEXT    │    │   CONTEXT    │                         │
│   └──────────────┘    └──────────────┘                         │
└─────────────────────────────────────────────────────────────────┘
```

---

## Context 1 — Identity & Access

**Responsibility:** Manages all users, roles, departments, branches, and agent-department assignments. Every other context consumes user identity but does not own it.

**Aggregate Roots:**
- `User` — system users (Admin, Manager, Agent, Customer portal users)
- `Department` — organizational units with optional custom business hours
- `Branch` — physical or logical locations

**Entities (within aggregates):**
- `AgentDepartment` — join between Agent and Department with IsPrimary flag
- `AgentSkill` — maps an agent to ticket categories they can handle

**Value Objects:**
- `UserRole` (enum) — Admin, Manager, Agent, Customer
- `AgentStatus` (enum) — Available, Busy, Away, Offline

**Domain Events Published:**
- `UserCreated` — other contexts may initialize default settings
- `UserDeactivated` — Ticketing context must reassign open tickets
- `AgentDepartmentAssigned` — Ticketing context updates routing rules
- `AgentStatusChanged` — SLA context updates assignment eligibility

**Consumes from:** Nothing (foundational context)

---

## Context 2 — Customer

**Responsibility:** Owns customer profiles, contact details, and tracks which customers are flagged as VIP. Does not own tickets — only the entity the ticket is about.

**Aggregate Root:**
- `Customer`

**Entities:**
- `CustomerContact` — secondary contacts (billing, technical, primary) per customer

**Value Objects:**
- `ContactType` (enum) — Primary, Billing, Technical, Other
- `Email` — validated string, unique across all customers

**Domain Events Published:**
- `CustomerCreated` — Integration context may sync to ERP
- `CustomerVipFlagged` — SLA & Automation context adds escalation trigger
- `CustomerExternalIdLinked` — Integration context records ERP mapping

**Consumes from:**
- `Integration` — to receive customer data synced from ERP

---

## Context 3 — Ticketing (Core Domain)

**Responsibility:** The heart of the system. Owns the full ticket lifecycle — creation, status transitions, department transfers, messages, and attachments. Enforces all business rules around who can change what and when.

**Aggregate Root:**
- `Ticket`

**Entities (within Ticket aggregate):**
- `TicketMessage` — all messages on the ticket (customer-facing + internal notes + @mentions)
- `TicketAttachment` — uploaded files linked to the ticket
- `TicketStatusHistory` — immutable log of every status change
- `TicketDepartmentHistory` — immutable log of every department transfer

**Supporting Entities (own aggregates):**
- `TicketCategory` — 2-level configurable category tree
- `TicketFieldDefinition` — custom field schema per department

**Value Objects:**
- `TicketPriority` (enum) — Critical(P1), High(P2), Medium(P3), Low(P4)
- `TicketStatus` (enum) — New, Assigned, InProgress, OnHold, Escalated, Resolved, Reopened, Closed
- `Channel` (enum) — Email, WhatsApp, SMS, LiveChat, WebForm, Portal
- `TicketNumber` — formatted human-readable ID (e.g., TKT-2025-00001)

**Domain Events Published:**
- `TicketCreated` — SLA context starts the SLA clock; Notification context alerts assigned agent
- `TicketAssigned` — Notification context sends assignment notification
- `TicketStatusChanged` — SLA context may pause/resume clock; Notification context alerts stakeholders
- `TicketEscalated` — SLA context records breach; Notification context alerts Manager/Admin
- `TicketDepartmentTransferred` — Notification context alerts new department
- `TicketFirstResponseRecorded` — SLA context stops first-response clock
- `TicketResolved` — CSAT context sends survey; SLA context stops resolution clock
- `TicketClosed` — Reporting context updates metrics
- `TicketReopened` — SLA context restarts relevant clocks
- `TicketMessageAdded` — Notification context processes @mentions
- `MentionCreated` — Notification context sends mention alert to mentioned user

**Consumes from:**
- `Identity & Access` — to validate assignee role and department membership
- `Customer` — to link ticket to customer record
- `SLA & Automation` — to display SLA status on ticket

---

## Context 4 — SLA & Automation

**Responsibility:** Tracks SLA clocks for every ticket, calculates deadlines using business hours, fires breach events, and drives auto-assignment and auto-escalation rules.

**Aggregate Root:**
- `SlaPolicy` — defines targets per priority (and optionally per department)
- `SlaClock` — active SLA timer per ticket

**Entities:**
- `BusinessHours` — global default + per-department overrides
- `BusinessHoliday` — holiday entries per BusinessHours config
- `EscalationRule` — configurable rules triggering escalation

**Value Objects:**
- `SlaTarget` — FirstResponseMinutes + ResolutionMinutes + UpdateFrequencyMinutes
- `SlaBreachLevel` (enum) — Warning (80%), Breach (100%), CriticalBreach (200%)

**Domain Events Published:**
- `SlaWarningTriggered` — Notification context alerts assigned agent
- `SlaBreached` — Ticketing context escalates ticket; Notification context alerts Manager
- `SlaCriticalBreached` — Ticketing context escalates to Admin; Notification context sends high-priority alert
- `TicketAutoAssigned` — Ticketing context records assignment
- `TicketAutoEscalated` — Ticketing context changes status to Escalated

**Consumes from:**
- `Ticketing` — reacts to TicketCreated, TicketStatusChanged, TicketFirstResponseRecorded
- `Identity & Access` — to know agent availability for auto-assignment

---

## Context 5 — Communication

**Responsibility:** Manages the actual messages flowing through each channel (Email, WhatsApp, SMS, Live Chat). Creates or updates tickets when inbound messages arrive. Delegates to the Integration context for provider-level sending/receiving.

**Aggregate Root:**
- `ChatSession` — an active live chat conversation before ticket creation

**Value Objects:**
- `Channel` (enum) — Email, WhatsApp, SMS, LiveChat, WebForm, Portal
- `MessageDirection` (enum) — Inbound, Outbound

**Domain Events Published:**
- `InboundMessageReceived` — Ticketing context creates or updates a ticket
- `ChatSessionStarted` — Notification context alerts available agents
- `ChatSessionInactive` — after 5-min timeout, Ticketing context auto-creates ticket
- `ChatBotHandoffRequested` — assigns chat to a human agent

**Consumes from:**
- `Integration` — provider-level email/SMS/WhatsApp events flow in
- `Ticketing` — to attach messages to the correct ticket
- `AI` — chatbot logic for first-response handling

---

## Context 6 — Knowledge Base

**Responsibility:** Owns all KB articles — creation, review workflow, visibility control, and full-text search. Agents author drafts; Managers/Editors publish.

**Aggregate Root:**
- `KbArticle`

**Value Objects:**
- `ArticleVisibility` (enum) — Internal, Public, Both
- `ArticleStatus` (enum) — Draft, Review, Published, Archived

**Domain Events Published:**
- `ArticlePublished` — AI context indexes article for suggestion engine
- `ArticleArchived` — AI context removes from suggestion index

**Consumes from:**
- `Identity & Access` — to enforce publish permissions (Agent vs. Editor vs. Manager)

---

## Context 7 — Notifications

**Responsibility:** Receives domain events from all other contexts and delivers the appropriate in-app notifications. Does not initiate business logic — only reacts.

**Aggregate Root:**
- `Notification`

**Value Objects:**
- `NotificationType` (enum) — TicketAssigned, SlaWarning, SlaBreach, Mention, TaskReminder, DepartmentTransfer, ChatHandoff, ArticleReviewRequested

**Domain Events Published:**
- `NotificationDelivered` (internal only — for read-receipt tracking)

**Consumes from:** Every other context — reacts to their events via Hangfire background jobs + SignalR push

---

## Context 8 — Agent Productivity

**Responsibility:** Personal agent tools — tasks/reminders linked to tickets or customers, and the quick-reply template library.

**Aggregate Roots:**
- `AgentTask` — personal to-do items with due dates and ticket/customer links
- `QuickReplyTemplate` — personal (agent) or global (admin) response templates

**Value Objects:**
- `TaskPriority` (enum) — High, Medium, Low
- `TaskStatus` (enum) — Pending, InProgress, Completed
- `TemplateScope` (enum) — Personal, Global

**Domain Events Published:**
- `TaskDue` — Notification context sends in-app reminder

**Consumes from:**
- `Identity & Access` — to scope templates and tasks to the correct agent
- `Ticketing` — to link tasks/templates to ticket context

---

## Context 9 — Reporting

**Responsibility:** Read-only. Builds aggregated views of ticket metrics, SLA performance, agent performance, and CSAT scores. Updated by consuming events from other contexts. No aggregate roots — this context is a set of read models (CQRS query side).

**Read Models:**
- `TicketMetricsSnapshot` — open tickets by status/priority/department
- `SlaPerformanceReport` — breach rates, avg response/resolution times
- `AgentPerformanceReport` — tickets handled, avg resolution, CSAT per agent
- `CsatReport` — scores per department, per agent, per category
- `DashboardKpi` — real-time KPI values pushed via SignalR

**Consumes from:** Ticketing, SLA & Automation, Identity & Access, Customer (CSAT) — all via domain events

---

## Context 10 — Integration

**Responsibility:** Anti-corruption layer between the CRM and all external systems — ERP (SAP Business One), Email provider (Gmail SMTP / production TBD), SMS gateway (Twilio), WhatsApp Business API (Twilio), and Azure OpenAI.

**No domain aggregates.** This context is infrastructure — it exposes interfaces that domain services depend on.

**Interfaces (Ports):**
- `IEmailProvider` — send/receive email
- `ISmsProvider` — send SMS
- `IWhatsAppProvider` — send/receive WhatsApp messages
- `IErpConnector` — pull customer data from SAP Business One; push ticket data
- `IAiProvider` — ticket summarization, reply suggestions, categorization, chatbot

**Domain Events Published:**
- `ErpCustomerSynced` — Customer context creates/updates customer record
- `InboundEmailReceived` → routed to Communication context
- `InboundWhatsAppReceived` → routed to Communication context
- `InboundSmsReceived` → routed to Communication context

---

## Context Interaction Map

```
                ┌──────────────────┐
                │  Identity &      │
                │  Access          │◀── All contexts check auth/roles
                └──────────────────┘

Customer ──CustomerCreated──▶ Integration (ERP sync)
Customer ──CustomerVipFlagged──▶ SLA & Automation

Ticketing ──TicketCreated──▶ SLA & Automation (start clock)
Ticketing ──TicketCreated──▶ Notifications (alert agent)
Ticketing ──TicketResolved──▶ Reporting (update metrics)
Ticketing ──TicketResolved──▶ CSAT (send survey)
Ticketing ──TicketMessageAdded──▶ Notifications (@mentions)
Ticketing ──TicketClosed──▶ Reporting

SLA & Automation ──SlaBreached──▶ Ticketing (escalate)
SLA & Automation ──SlaBreached──▶ Notifications (alert manager)
SLA & Automation ──TicketAutoAssigned──▶ Ticketing

Communication ──InboundMessageReceived──▶ Ticketing (create/update ticket)
Communication ──ChatSessionInactive──▶ Ticketing (create ticket from transcript)

KnowledgeBase ──ArticlePublished──▶ AI (index for suggestions)

AI ──(suggestion)──▶ Ticketing (agent reviews, confirms)

Notifications ──(SignalR push)──▶ Agent Dashboard (real-time)

Integration ──ErpCustomerSynced──▶ Customer (upsert record)
Integration ──InboundEmailReceived──▶ Communication
```

---

## Bounded Context Boundaries — Key Rules

1. **No cross-context object references** — contexts share only IDs (Guids), never objects
2. **Events are the integration mechanism** — synchronous calls allowed only within a context
3. **Each context has its own DB schema** — implemented as separate EF Core `DbContext` classes sharing one SQL Server database (schema-per-context)
4. **AI is always advisory** — AI context never directly changes ticket data; it suggests and the agent confirms
5. **Notifications are reactive** — Notification context never initiates business actions; it only reacts to events from other contexts
