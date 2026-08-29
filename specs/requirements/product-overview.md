# Product Overview — AZM Squad Customer Support CRM

> **Status:** Draft — Phase 1 (Requirements Analysis)
> **Version:** 0.1.0
> **Date:** 2026-08-24

---

## Document Purpose

This document catalogs every functional requirement extracted from the product brief, assigns unique traceability IDs, maps inter-module dependencies, and flags known ambiguities for follow-up in `open-questions.md`.

---

## Requirement ID Convention

| Prefix | Module |
|--------|--------|
| REQ-CUST | Customer Management |
| REQ-TICK | Ticket Management |
| REQ-COMM | Communication Channels |
| REQ-AGNT | Agent Dashboard |
| REQ-SLA  | SLA & Automation |
| REQ-KB   | Knowledge Base |
| REQ-AI   | AI Features |
| REQ-CPORT | Customer Portal |
| REQ-RPT  | Reports & Management |
| REQ-SEC  | Security & Administration |
| REQ-INT  | Integrations |
| REQ-PLT  | Platform |

---

## Module 1 — Customer Management

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-CUST-001 | The system shall maintain a profile record for each customer | Must Have | OQ-001 |
| REQ-CUST-002 | Each customer profile shall store contact details (name, email, phone, address) | Must Have | OQ-002 |
| REQ-CUST-003 | The system shall record and display a chronological interaction history per customer, spanning all channels | Must Have | — |
| REQ-CUST-004 | Agents shall be able to add free-text notes to a customer record | Must Have | — |
| REQ-CUST-005 | Agents shall be able to upload and attach files to a customer record | Must Have | OQ-003 |
| REQ-CUST-006 | Customer records shall be searchable by name, email, phone, and any custom identifier | Should Have | OQ-001 |

**Dependencies:** None (foundational module)

---

## Module 2 — Ticket Management

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-TICK-001 | Agents and the system shall be able to create a support ticket on behalf of a customer | Must Have | — |
| REQ-TICK-002 | Each ticket shall be associated with exactly one customer record | Must Have | OQ-001 |
| REQ-TICK-003 | Each ticket shall have a category (type/topic area) | Must Have | OQ-004 |
| REQ-TICK-004 | Each ticket shall have a priority level | Must Have | OQ-005 |
| REQ-TICK-005 | Tickets shall be assignable to an individual agent | Must Have | — |
| REQ-TICK-006 | Tickets shall have a tracked status with defined lifecycle transitions | Must Have | OQ-006 |
| REQ-TICK-007 | The system shall support ticket escalation, either manually or automatically | Must Have | OQ-007 |
| REQ-TICK-008 | The system shall maintain a complete, immutable history/audit trail for every ticket | Must Have | — |
| REQ-TICK-009 | Tickets shall be creatable from any supported communication channel | Must Have | — |
| REQ-TICK-010 | Agents shall be able to merge duplicate tickets | Should Have | — |
| REQ-TICK-011 | Agents shall be able to split a ticket into child tickets | Could Have | — |

**Dependencies:** REQ-CUST-001 (ticket requires a customer)

---

## Module 3 — Communication Channels

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-COMM-001 | The system shall send and receive customer communication via Email | Must Have | OQ-008 |
| REQ-COMM-002 | The system shall send and receive customer communication via WhatsApp | Must Have | OQ-009 |
| REQ-COMM-003 | The system shall provide a real-time Live Chat capability on the customer portal | Must Have | OQ-010 |
| REQ-COMM-004 | The system shall send outbound customer communication via SMS | Must Have | OQ-011 |
| REQ-COMM-005 | The system shall accept inbound customer contact via embeddable Web Forms | Must Have | OQ-012 |
| REQ-COMM-006 | All inbound channel messages shall automatically create or update a corresponding ticket | Must Have | — |
| REQ-COMM-007 | All channel communications shall be stored in the ticket thread and customer interaction history | Must Have | — |

**Dependencies:** REQ-TICK-001 (channels produce tickets), REQ-INT-003, REQ-INT-004, REQ-INT-005

---

## Module 4 — Agent Dashboard

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-AGNT-001 | The agent dashboard shall display a prioritized list of tickets assigned to the logged-in agent | Must Have | — |
| REQ-AGNT-002 | The agent dashboard shall display contextual customer information when a ticket is selected | Must Have | — |
| REQ-AGNT-003 | Agents shall be able to create personal tasks and reminders linked to tickets or customers | Should Have | OQ-013 |
| REQ-AGNT-004 | The system shall provide a quick-reply template library for common responses | Should Have | OQ-014 |
| REQ-AGNT-005 | The system shall support team collaboration features within a ticket (internal notes, @mentions) | Must Have | OQ-015 |
| REQ-AGNT-006 | Agents shall receive real-time notifications for new assignments, SLA warnings, and updates | Must Have | — |

**Dependencies:** REQ-TICK-001, REQ-CUST-001, REQ-SLA-001

---

## Module 5 — SLA & Automation

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-SLA-001 | The system shall define and track a first-response time SLA target per priority/category | Must Have | OQ-005, OQ-016 |
| REQ-SLA-002 | The system shall define and track a resolution time SLA target per priority/category | Must Have | OQ-005, OQ-016 |
| REQ-SLA-003 | The system shall automatically assign incoming tickets to agents based on configurable rules | Should Have | OQ-017 |
| REQ-SLA-004 | The system shall automatically escalate tickets that breach or are at risk of breaching SLA | Must Have | OQ-007 |
| REQ-SLA-005 | The system shall send alerts and notifications to agents and supervisors for SLA events | Must Have | OQ-018 |
| REQ-SLA-006 | SLA clocks shall support business-hours-only calculation | Should Have | OQ-019 |
| REQ-SLA-007 | SLA policies shall be configurable per department and/or branch | Should Have | OQ-020 |

**Dependencies:** REQ-TICK-001, REQ-TICK-004, REQ-TICK-006, REQ-PLT-006, REQ-PLT-007

---

## Module 6 — Knowledge Base

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-KB-001 | The system shall support authoring and publishing FAQ articles | Must Have | — |
| REQ-KB-002 | The system shall support authoring and publishing long-form help articles | Must Have | — |
| REQ-KB-003 | The system shall support authoring and publishing step-by-step solution guides | Must Have | — |
| REQ-KB-004 | The knowledge base shall have full-text search | Must Have | — |
| REQ-KB-005 | Articles shall support both Arabic and English content | Must Have | REQ-PLT-001, REQ-PLT-002 |
| REQ-KB-006 | Articles shall have a published/draft/archived lifecycle | Should Have | — |
| REQ-KB-007 | The knowledge base shall be accessible to both agents and customers | Must Have | OQ-021 |

**Dependencies:** REQ-PLT-001, REQ-PLT-002

---

## Module 7 — AI Features

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-AI-001 | The system shall generate a concise AI summary of each ticket's thread on demand | Should Have | OQ-022 |
| REQ-AI-002 | The system shall suggest AI-generated reply drafts to the agent based on ticket context | Should Have | OQ-022 |
| REQ-AI-003 | The system shall automatically categorize new incoming tickets using AI | Should Have | OQ-022, OQ-023 |
| REQ-AI-004 | The system shall suggest relevant knowledge base articles as solutions for open tickets | Should Have | OQ-022 |
| REQ-AI-005 | The system shall provide an AI-powered chatbot for customer self-service | Could Have | OQ-024 |

**Dependencies:** REQ-TICK-001, REQ-KB-001, OQ-022 (AI provider decision)

---

## Module 8 — Customer Portal

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-CPORT-001 | Customers shall be able to register and log in to a self-service portal | Must Have | OQ-025 |
| REQ-CPORT-002 | Authenticated customers shall be able to submit new support tickets via the portal | Must Have | — |
| REQ-CPORT-003 | Authenticated customers shall be able to track the real-time status of their submitted tickets | Must Have | — |
| REQ-CPORT-004 | Authenticated customers shall be able to view their full interaction history with support | Must Have | — |
| REQ-CPORT-005 | Customers shall be able to access the knowledge base / FAQ from the portal | Must Have | — |
| REQ-CPORT-006 | Customers shall be able to submit satisfaction feedback after ticket resolution | Should Have | OQ-026 |

**Dependencies:** REQ-TICK-001, REQ-CUST-001, REQ-KB-007, REQ-COMM-003

---

## Module 9 — Reports & Management

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-RPT-001 | The system shall generate reports on ticket volume, status distribution, and channel breakdown | Must Have | — |
| REQ-RPT-002 | The system shall generate SLA compliance and breach reports | Must Have | — |
| REQ-RPT-003 | The system shall generate agent performance reports (tickets handled, resolution time, CSAT) | Must Have | — |
| REQ-RPT-004 | The system shall track and report customer satisfaction scores | Must Have | OQ-026 |
| REQ-RPT-005 | The system shall provide real-time management dashboards with configurable KPIs | Must Have | OQ-027 |
| REQ-RPT-006 | Reports shall be filterable by date range, department, branch, and agent | Should Have | — |
| REQ-RPT-007 | Reports shall be exportable (CSV, Excel, PDF) | Should Have | — |

**Dependencies:** REQ-TICK-001, REQ-SLA-001, REQ-AGNT-001, REQ-PLT-006, REQ-PLT-007

---

## Module 10 — Security & Administration

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-SEC-001 | The system shall support user account creation, management, and deactivation | Must Have | — |
| REQ-SEC-002 | The system shall implement role-based access control (RBAC) | Must Have | OQ-028 |
| REQ-SEC-003 | Each role shall have a configurable set of permissions at the feature level | Must Have | OQ-028 |
| REQ-SEC-004 | The system shall record a tamper-proof audit log of all significant user actions | Must Have | OQ-029 |
| REQ-SEC-005 | Administrators shall be able to configure system-wide settings through an admin panel | Must Have | OQ-030 |
| REQ-SEC-006 | Passwords shall meet configurable complexity requirements | Must Have | — |
| REQ-SEC-007 | Sessions shall expire after a configurable idle timeout | Must Have | — |

**Dependencies:** All modules depend on REQ-SEC-001, REQ-SEC-002

---

## Module 11 — Integrations

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-INT-001 | The system shall expose a documented RESTful API for external system integration | Must Have | — |
| REQ-INT-002 | The system shall provide integration capability with ERP systems | Should Have | OQ-031 |
| REQ-INT-003 | The system shall integrate with at least one external email provider | Must Have | OQ-008 |
| REQ-INT-004 | The system shall integrate with at least one SMS gateway provider | Must Have | OQ-011 |
| REQ-INT-005 | The system shall integrate with the WhatsApp Business API | Must Have | OQ-009 |
| REQ-INT-006 | The integration layer shall support webhook-based event delivery to external systems | Should Have | — |

**Dependencies:** REQ-COMM-001–005 depend on REQ-INT-003, REQ-INT-004, REQ-INT-005

---

## Module 12 — Platform

| ID | Requirement | Priority | Ambiguity |
|----|-------------|----------|-----------|
| REQ-PLT-001 | All user-facing text shall be available in Arabic | Must Have | — |
| REQ-PLT-002 | All user-facing text shall be available in English | Must Have | — |
| REQ-PLT-003 | The UI shall render in RTL layout when Arabic is selected | Must Have | — |
| REQ-PLT-004 | The application shall be fully functional in modern web browsers | Must Have | OQ-032 |
| REQ-PLT-005 | The application UI shall be responsive and usable on mobile devices | Must Have | OQ-033 |
| REQ-PLT-006 | The system shall support multiple departments with isolated data views | Must Have | OQ-020 |
| REQ-PLT-007 | The system shall support multiple branches with isolated data views | Must Have | OQ-020 |
| REQ-PLT-008 | The system shall support custom branding (logo, colors) configurable per tenant or branch | Should Have | OQ-034 |

**Dependencies:** All modules depend on REQ-PLT-001–003 (i18n/RTL)

---

## Summary

| Module | Req Count | Must Have | Should Have | Could Have |
|--------|-----------|-----------|-------------|------------|
| Customer Management | 6 | 5 | 1 | 0 |
| Ticket Management | 11 | 9 | 1 | 1 |
| Communication Channels | 7 | 7 | 0 | 0 |
| Agent Dashboard | 6 | 4 | 2 | 0 |
| SLA & Automation | 7 | 4 | 3 | 0 |
| Knowledge Base | 7 | 6 | 1 | 0 |
| AI Features | 5 | 0 | 4 | 1 |
| Customer Portal | 6 | 5 | 1 | 0 |
| Reports & Management | 7 | 5 | 2 | 0 |
| Security & Administration | 7 | 7 | 0 | 0 |
| Integrations | 6 | 4 | 2 | 0 |
| Platform | 8 | 7 | 1 | 0 |
| **TOTAL** | **83** | **63** | **18** | **2** |

---

## Cross-Cutting Dependency Map

```
REQ-PLT (i18n, RTL, Multi-dept, Multi-branch)
    └── applies to ALL modules

REQ-SEC (Auth, RBAC, Audit)
    └── gates ALL modules

REQ-CUST (Customer profiles)
    └── REQ-TICK (Tickets)
        ├── REQ-COMM (Channels feed tickets)
        ├── REQ-AGNT (Dashboard shows tickets)
        ├── REQ-SLA  (SLA applied to tickets)
        ├── REQ-CPORT (Portal submits/tracks tickets)
        └── REQ-RPT  (Reports on tickets)

REQ-KB (Knowledge Base)
    ├── REQ-AI-004 (AI suggests from KB)
    └── REQ-CPORT-005 (Portal exposes KB)

REQ-INT (External integrations)
    └── REQ-COMM (Channels depend on providers)
```
