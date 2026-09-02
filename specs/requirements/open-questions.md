# Open Questions — AZM Squad Customer Support CRM

> **Status:** COMPLETE — all 34 questions answered (2026-08-24)
> **Version:** 0.1.0
> **Date:** 2026-08-24

---

## How to Use This Document

Each question is assigned a unique ID (OQ-XXX) referenced from `product-overview.md`. Questions are grouped by domain. Before each Phase's implementation begins, all questions tagged **BLOCKING** for that phase must be answered.

| Status | Meaning |
|--------|---------|
| OPEN | No answer yet |
| ANSWERED | Stakeholder has responded |
| DEFERRED | Intentionally postponed to a later phase |

---

## Section 1 — Customer Data

### OQ-001 — Customer Uniqueness & Identity
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-CUST-001, REQ-CUST-006, REQ-TICK-002

**Answer:**
- **Primary key:** `CustomerId` — auto-generated GUID
- **Business unique key:** `Email` — must be unique across the entire system
- **ERP integration:** Optional `ExternalId` field (nullable string) for mapping to ERP/legacy systems
- **Multiple contacts:** A customer record supports multiple `CustomerContact` entries (e.g., Primary, Billing, Technical). Each contact has its own name, email, and phone. The contact email is separate from the customer-level unique email.
- **Cross-department:** A single customer record is shared across all departments (no per-department duplication)

---

### OQ-002 — Customer Contact Fields
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-CUST-002

**Answer:**

| Field | Required | Type |
|-------|----------|------|
| FullName | ✅ Required | string |
| Email | ✅ Required (unique) | email |
| Phone | ✅ Required | string |
| CompanyName | Optional | string |
| JobTitle | Optional | string |
| Country | Optional | dropdown — Middle East countries (SA, UAE, KW, QA, OM, BH, JO, EG, …) |
| City | Optional | free text |
| Street | Optional | free text |
| BuildingNumber | Optional | free text |

- **Custom fields per department:** ❌ No custom customer contact fields. Customer profile schema is global and fixed for all departments.
- *Ticket-level custom fields per department are a separate feature — see OQ-030.*

---

### OQ-003 — File Attachments Policy
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-CUST-005, REQ-TICK-001

**Answer:**
- **Allowed types:** PDF, JPG, PNG, DOCX, XLSX, TXT
- **Blocked types:** Executables (.exe, .bat, .sh), archives (.zip, .rar) — unless virus-scanned
- **Max size per file:** 5 MB
- **Max total per ticket:** 10 MB
- **Max total per customer:** 50 MB
- **Retention:** 2 years (configurable); auto-delete after expiry
- **Manual deletion:** Admin can manually delete; deletion is logged in audit trail
- **Storage backend:** S3-compatible object storage

---

## Section 2 — Ticket Management

### OQ-004 — Ticket Categories
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-TICK-003

**Answer:**
- **Configurable:** Categories are managed by Admin users in Settings — not hardcoded enums
- **Hierarchy:** Exactly 2 levels — Parent → Child (no deeper nesting allowed)
  - Example: `Technical Support` (Parent) → `Hardware`, `Software`, `Network` (Children)
  - Example: `Billing` (Parent) → `Invoice`, `Payment`, `Refund` (Children)
- **Scope:** Shared across all departments (global category tree)
- **Seeding:** Pre-seeded at launch with ~5–8 parent categories, 3–5 children each

---

### OQ-005 — Ticket Priority Levels
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-TICK-004, REQ-SLA-001, REQ-SLA-002

**Answer:**

| Priority | Code | Color | Response SLA | Resolution SLA |
|----------|------|-------|--------------|----------------|
| Critical | P1 | Red | 1 hour | 4 hours |
| High | P2 | Orange | 4 hours | 24 hours |
| Medium | P3 | Yellow | 24 hours | 48 hours |
| Low | P4 | Green | 48 hours | 5 business days |

*Refined first-response targets defined in OQ-016. Priority is agent/system-assigned — customers cannot set priority.*

---

### OQ-006 — Ticket Status Lifecycle
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-TICK-006

**Answer:**

**Statuses:** `New`, `Assigned`, `InProgress`, `OnHold`, `Escalated`, `Resolved`, `Reopened`, `Closed`

**Valid transitions:**

| From | To | Allowed By |
|------|----|------------|
| New | Assigned | Manager, Admin |
| New | Closed | Admin (reject invalid ticket) |
| Assigned | InProgress | Agent (self-assigned ticket) |
| Assigned | Resolved | Agent, Manager |
| InProgress | Resolved | Agent, Manager |
| InProgress | Escalated | Agent, Manager |
| InProgress | OnHold | Agent, Manager (waiting for customer) |
| OnHold | InProgress | Agent, Manager, System (customer replied) |
| Escalated | InProgress | Manager, Admin |
| Resolved | Closed | Agent, Manager, Customer |
| Resolved | Reopened | Agent, Manager, Customer |
| Closed | Reopened | Agent, Manager, Admin |

**Reopened** feeds back into `InProgress` (auto-transition after reopen assignment).

---

### OQ-007 — Escalation Rules
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-TICK-007, REQ-SLA-004

**Answer:**

**Escalation triggers (beyond SLA breach):**
- VIP customer flag on customer record
- Keyword detection in ticket subject/body (e.g., "urgent", "critical", "unacceptable")
- Repeated contacts — 3+ customer updates with no agent response
- Manager or Admin manually escalates
- High-value customer (spending threshold — configurable)
- Security or privacy-sensitive ticket flag

**Escalation chain (multi-level):**
```
Agent → Manager → Admin
```
*"Department Head" in requirements maps to the Manager role (OQ-028).*

**Priority auto-escalation:** Yes — each escalation raises priority by one level:

| Current Priority | After Escalation |
|-----------------|-----------------|
| Low (P4) | Medium (P3) |
| Medium (P3) | High (P2) |
| High (P2) | Critical (P1) |
| Critical (P1) | Admin notification (no further raise) |

---

## Section 3 — Communication Channels

### OQ-008 — Email Provider
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-COMM-001, REQ-INT-003

**Answer:**
- **Development:** Gmail SMTP — `smtp.gmail.com:587` (TLS), App Password authentication, ~100–500 emails/day limit (sufficient for dev)
- **Production:** TBD — decide before Phase 3 production deployment

---

### OQ-009 — WhatsApp Provider
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-COMM-002, REQ-INT-005

**Answer:**
- **Development:** Twilio Free Trial — 30-day trial, 100 free WhatsApp messages, pre-configured sandbox, no credit card required
- **Production:** TBD — decide before Phase 3 production deployment

---

### OQ-010 — Live Chat Scope
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-COMM-003

**Answer:**
- **First message:** AI chatbot handles the first message — attempts intent detection, suggests KB articles, escalates to agent if unresolved
- **Auto-convert to ticket:** ✅ Yes — after 5 minutes of inactivity; chat transcript becomes the ticket description
- **Development library options (decision pending):** Tencent RTC Chat (free tier, 1,000 MAU) or Twilio Chat (trial tier) — a specific library must be chosen before Phase 3 implementation begins
- **Production:** TBD

---

### OQ-011 — SMS Gateway Provider
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-COMM-004, REQ-INT-004

**Answer:**
- **Development:** Twilio Free Trial — 30-day trial, 100 free SMS messages, global coverage, no credit card required
- **Production:** TBD — decide before Phase 3 production deployment

---

### OQ-012 — Web Form Behavior
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-COMM-005

**Answer:**
- **Scope:** Portal-only — not embeddable on external websites
- **Authentication:** Customer must be logged in to submit (portal requires authentication per OQ-025)
- **Implication:** Web form is effectively the "New Ticket" form inside the customer portal — no standalone embed widget needed

---

## Section 4 — Agent & Team Features

### OQ-013 — Tasks & Reminders Scope
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-AGNT-003

**Answer:**
- **Scope:** Personal to the creating agent only — no sharing or assignment to other agents
- **Calendar sync:** ❌ None in v1
- **Features:** Due date/time, priority (High/Medium/Low), notes/description, mark complete, link to ticket or customer
- **Notifications:** In-app reminder before due time

---

### OQ-014 — Quick Reply Templates
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-AGNT-004

**Answer:**
- **Agent:** Can create personal templates — visible only to that agent
- **Admin:** Can create global templates — visible to all agents
- **Dynamic placeholders:** ✅ Yes — `{{customer_name}}`, `{{ticket_id}}`, `{{ticket_subject}}`, `{{agent_name}}`, `{{department}}`, `{{ticket_status}}`, `{{ticket_priority}}`
- **Categories:** Templates organized by category (e.g., Greeting, Billing, Support, Closing)
- **Search:** Searchable by title and content

---

### OQ-015 — Team Collaboration
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-AGNT-005

**Answer:**
- **@mentions:** ✅ Yes — type `@username` in any ticket note to notify a specific agent or manager; mentioned user receives in-app notification; mention appears in ticket history
- **Ticket watchers:** ❌ No
- **Real-time presence:** ❌ No (seeing who else is viewing a ticket not required)
- **Collaboration model:** Notes + @mentions is the collaboration mechanism

---

## Section 5 — SLA & Business Rules

### OQ-016 — SLA Target Values
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-SLA-001, REQ-SLA-002

**Answer:**

| Priority | First Response | Resolution | Update Frequency |
|----------|---------------|------------|-----------------|
| Critical (P1) | 15 minutes | 4 hours | Every 30 minutes |
| High (P2) | 30 minutes | 24 hours | Every 2 hours |
| Medium (P3) | 2 hours | 48 hours | Daily |
| Low (P4) | 4 hours | 5 business days | Every 2 business days |

**Breach escalation tiers:**
- **Warning:** 80% of SLA time elapsed → alert to assigned agent
- **Breach:** 100% elapsed → escalate to Manager + email notification
- **Critical breach:** 200% elapsed → escalate to Admin + high-priority notification

---

### OQ-017 — Auto-Assignment Rules
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-SLA-003

**Answer (priority order):**

1. **Skills-based (primary):** Ticket category maps to required skill; only agents with that skill are candidates. Example: "Network" tickets → agents tagged with Network skill only.
2. **Round-robin (secondary):** If no skill is specified, distribute evenly among available agents in the department.
3. **Agent pull queue (fallback):** Unassigned tickets appear in an "Unassigned" queue; agents can manually claim them.
4. **Escalate to Manager (overflow):** If no agent accepts within a configurable timeout (default 15 min), escalate to Manager.

**Agent availability status:** Available, Busy, Away, Offline
- Auto-away after 10 minutes of inactivity
- Tickets not auto-assigned to agents with status = Offline

---

### OQ-018 — Notification Channels for SLA Alerts
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-SLA-005

**Answer:**
- **Development:** In-app notifications only via SignalR real-time push; email/SMS mocked
- **Production:** TBD — multi-channel delivery (email, SMS, WhatsApp to supervisor) to be decided before Phase 3 production

---

### OQ-019 — Business Hours Configuration
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-SLA-006

**Answer:**
- **Global default:** Sunday–Thursday, 08:00–18:00, KSA timezone (UTC+3)
- **Per-department override:** Each department can define custom hours (e.g., Billing: 08:00–16:00; Technical Support: 24/7)
- **Time zone:** Configurable system setting; default KSA UTC+3
- **Holidays:** Configurable at both global and per-department level
- **SLA clock behavior:** Clock pauses outside business hours and resumes at next open hour

---

### OQ-020 — Multi-Department / Multi-Branch Data Model
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-PLT-006, REQ-PLT-007, REQ-SLA-007

**Answer:**

**Tenancy model:** Single organization — one company with multiple departments and branches. Not SaaS.

**Agent department membership:**
- Each agent has exactly one **Primary Department** (used as the default for ticket assignment)
- An agent can additionally be assigned to one or more **Secondary Departments**
- The agent's dashboard shows tickets from all departments they are assigned to (primary + secondary)

**Ticket transfers between departments:**
- **Agent-initiated:** Only Managers (Level 2) and Admins (Level 1) can transfer a ticket to another department. Agents can only transfer within their own department.
- **Auto-escalation:** SLA/escalation rules may reassign a ticket to a different department automatically.
- **Audit trail (mandatory):** Every department change must be logged with:
  - Who initiated the transfer
  - Timestamp
  - Reason for transfer

**Cross-department reporting:** Admins and Managers can view cross-department reports. Agent reports are scoped to their assigned departments only.

---

## Section 6 — Knowledge Base

### OQ-021 — Knowledge Base Access Control
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-KB-007

**Answer:**
- **Article visibility:** Each article has a visibility flag — `Internal` (agents/managers/admins only), `Public` (customers on portal), or `Both`
- **Publish workflow:** Draft → Review → Published → Archived
- **Who can author:** Any agent (creates draft)
- **Who can publish:** Manager, Admin, or agents designated as Editors by Admin
- **Full-text search:** ✅ On title + content, available to all who have access to the article

---

## Section 7 — AI Features

### OQ-022 — AI Provider & Model Selection
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-AI-001–005

**Answer:**
- **Development:** Azure OpenAI free tier — GPT-4o-mini or GPT-3.5-Turbo. Alternative: OpenAI API pay-as-you-go.
- **Production:** TBD
- **Data residency:** ✅ MANDATORY — ticket content may contain customer PII. Azure OpenAI must be deployed in UAE or Europe region. US-based OpenAI API endpoints are NOT permitted. Self-hosted model may be required in production if cloud residency cannot be guaranteed.

---

### OQ-023 — AI Auto-Categorization Confidence Threshold
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-AI-003

**Answer:** Suggest-only — never auto-apply without agent confirmation.

| Confidence | UI Behaviour |
|------------|-------------|
| > 80% (High) | "Likely category: Technical Support → Hardware" |
| 50–80% (Medium) | "Suggested category: Technical Support → Hardware" |
| < 50% (Low) | "Unable to determine category" |

Agent reviews and confirms or overrides the suggestion. Rationale: reduces mis-categorization risk; agent stays in control.

---

### OQ-024 — AI Chatbot Scope
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-AI-005

**Answer:**
- **Placement:** Both — customer-facing on the portal AND internal agent assistant in the dashboard
- **Hand-off triggers (bot → human agent):**
  - Bot cannot understand the query
  - Complex or sensitive topic detected
  - Customer explicitly requests a human
  - 3 consecutive failed resolution attempts
- **Auto-convert:** Chat session converts to ticket after 5 minutes of inactivity (transcript becomes ticket description — same as OQ-010)

---

## Section 8 — Customer Portal

### OQ-025 — Customer Portal Authentication
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-CPORT-001

**Answer:**
- **Development:** Email + password with email verification via Gmail SMTP; Phone OTP optional (skipped in dev)
- **Production:** TBD — SSO (Google/Microsoft) and/or Phone OTP may be added later

---

### OQ-026 — Customer Satisfaction Measurement
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-CPORT-006, REQ-RPT-004

**Answer:**
- **v1 method:** CSAT only (NPS deferred to future enhancement)
- **Trigger:** Auto-triggered when ticket is marked Closed — email sent to customer + in-app survey in customer portal
- **Rating:** 1–5 scale (1 = Very Dissatisfied → 5 = Very Satisfied)
- **Comment:** ✅ Optional free-text comment box
- **Reporting:** Response rate, per-agent CSAT score (average), per-department CSAT score

---

## Section 9 — Reports & Dashboards

### OQ-027 — Management Dashboard KPIs
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-RPT-005

**Answer:**

**Core KPIs:**
- Open tickets — by priority and department
- SLA breach rate (%)
- Average first response time — by priority
- Average resolution time — by priority
- CSAT score — per department and per agent
- Agent utilization (% of time handling tickets)
- Ticket volume trend — daily / weekly

**Additional KPIs:**
- Tickets created vs. resolved (daily)
- Escalation rate (% of tickets escalated)
- Agent workload — tickets assigned vs. capacity
- Department performance — response and resolution times
- Customer satisfaction — by category and department

**Refresh:** Real-time via SignalR push — live dashboards with no manual refresh needed.
**Historical filters:** Daily / weekly / monthly date range selectors.

---

## Section 10 — Security & Administration

### OQ-028 — Roles & Permissions Definition
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-SEC-002, REQ-SEC-003

**Answer:**

**Roles (4 total, simplified from initial proposal):**

| Role | Level | Description | Scope |
|------|-------|-------------|-------|
| Admin | 1 | Full system control + IT configuration (Super Admin + IT Admin merged) | System-wide |
| Manager | 2 | Department management + team oversight (Manager + Supervisor merged) | Department-level full access |
| Agent | 3 | Handle tickets, communicate with customers | Assigned tickets + own department tickets |
| Customer | 4 | Portal access, submit and view own tickets | Own tickets only |

**Permission Matrix:**

| Action | Admin | Manager | Agent | Customer |
|--------|-------|---------|-------|----------|
| Manage users | ✅ | ❌ | ❌ | ❌ |
| System configuration | ✅ | ❌ | ❌ | ❌ |
| View all tickets (cross-dept) | ✅ | ✅ | ❌ | ❌ |
| View department tickets | ✅ | ✅ | ✅ | ❌ |
| Assign tickets | ✅ | ✅ | ❌ | ❌ |
| Transfer between departments | ✅ | ✅ | ❌ | ❌ |
| Handle tickets (reply, update) | ✅ | ✅ | ✅ | ❌ |
| View reports & dashboards | ✅ | ✅ | ✅ | ❌ |
| Manage SLA rules | ✅ | ✅ | ❌ | ❌ |
| View own tickets | ✅ | ✅ | ✅ | ✅ |
| Submit tickets | ✅ | ✅ | ✅ | ✅ |

*Note: Agent sees only tickets assigned to them, not all department tickets they are not assigned to.*

**Authorization approach:** Standard RBAC is sufficient. No ABAC needed at this time.

---

### OQ-029 — Audit Log Requirements
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-SEC-004

**Answer:**
- **Retention:** 2 years
- **Exportable:** Yes — CSV, Excel, PDF formats
- **Regulatory framework:** None (internal compliance only)

**Each audit entry captures:**

| Field | Description |
|-------|-------------|
| Who | UserId + UserName |
| What | Action performed (create/update/delete/login/etc.) |
| When | UTC timestamp |
| Where | IP address + device/browser info |
| OldValue | JSON snapshot of previous state |
| NewValue | JSON snapshot of new state |
| Reason | Optional — for actions that prompt for justification |

---

### OQ-030 — System Configuration Scope
**Status:** ANSWERED (revised) — 2026-08-24 | **Related:** REQ-SEC-005

**Answer:** All items below are configurable by Admin only:

| Setting | Configurable? | Notes |
|---------|--------------|-------|
| SLA targets per priority | ✅ | Response time, resolution time, update frequency |
| Business hours & holidays | ✅ | Global + per-department overrides |
| Ticket categories & priorities | ✅ | 2-level hierarchy, seeded at launch |
| Email / SMS / WhatsApp credentials | ✅ | Provider API keys & settings |
| Branding (logo, colors) | ✅ | Per branch or global |
| AI feature on/off toggle | ✅ | Per AI feature individually |
| User roles & permissions | ✅ | Role assignments |
| Department / Branch management | ✅ | Create, edit, deactivate |
| Ticket custom fields per department | ✅ | Field definitions per department (see below) |
| Customer contact fields | ❌ | Fixed schema — not configurable (see OQ-002) |

**Ticket custom fields — detail:**
- Admin defines additional fields that appear on ticket forms, scoped per department (and optionally per category)
- **Field types:** Text, Number, Dropdown, Date, Checkbox, Textarea
- **Examples:**
  - Technical Support: "Serial Number" (Text), "Product Version" (Dropdown), "Error Code" (Text)
  - Billing: "Invoice Number" (Text), "Amount" (Number), "Payment Method" (Dropdown)
- **Storage:** Field definitions stored in a `TicketFieldDefinition` table; values stored as JSON in `TicketFieldValues` column on the ticket record
- Fields appear on ticket creation and update forms dynamically based on the selected department

---

## Section 11 — Integrations

### OQ-031 — ERP Integration Scope
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-INT-002

**Answer:**
- **Primary ERP:** SAP Business One. Secondary: Oracle ERP (if required).
- **Development:** Simulated / mocked API endpoints (no real ERP connection needed in dev)
- **Production:** REST API (real-time) + daily batch sync

**Data flows:**

| Direction | Data | Frequency |
|-----------|------|-----------|
| ERP → CRM | Customer: Name, Email, Phone, Company, Address, VAT/CR Number | Real-time on-demand pull |
| CRM → ERP | Ticket: Status, resolution notes, SLA data | Daily batch |
| ERP → CRM | Orders / Billing | ❌ Not in v1 |

---

## Section 12 — Platform & Deployment

### OQ-032 — Browser Support Matrix
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-PLT-004

**Answer:**

| Browser | Support |
|---------|---------|
| Chrome | ✅ Latest 2 versions |
| Edge | ✅ Latest 2 versions |
| Firefox | ✅ Latest 2 versions |
| Safari | ✅ 16+ |
| Opera | ✅ Latest 2 versions |
| Internet Explorer | ❌ Not supported |
| Safari 15 and below | ❌ Not supported |

- **Minimum resolution:** 1024×768
- **Mobile browsers:** iOS Safari + Android Chrome (responsive design)

---

### OQ-033 — Mobile Strategy
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-PLT-005

**Answer:**
- **v1 (this project):** Responsive Angular PWA — works on iOS Safari and Android Chrome for both agents and customers
- **Future (out of scope):** No native iOS/Android apps planned

---

### OQ-034 — Custom Branding Scope
**Status:** ANSWERED — 2026-08-24 | **Related:** REQ-PLT-008

**Answer:**
- **Custom domain:** ❌ No (v1)
- **Custom email sender:** ❌ No (v1) — uses system email address

**Configurable branding elements (Admin panel):**

| Element | Applies To |
|---------|-----------|
| Company logo (image upload) | Customer portal, agent dashboard, emails |
| Primary color | Customer portal, agent dashboard |
| Secondary color | Customer portal, agent dashboard |
| Favicon | Customer portal, agent dashboard |
| Portal header text | Customer portal |
| Portal welcome message | Customer portal |
| Email template branding | Emails sent to customers (logo + colors) |

---

## Summary Table

| OQ ID | Domain | Blocking Phase | Status |
|-------|--------|----------------|--------|
| OQ-001 | Customer | Phase 2 | **ANSWERED** |
| OQ-002 | Customer | Phase 2 | **ANSWERED** |
| OQ-003 | Customer | Phase 2 | **ANSWERED** |
| OQ-004 | Ticket | Phase 2 | **ANSWERED** |
| OQ-005 | Ticket | Phase 2 | **ANSWERED** |
| OQ-006 | Ticket | Phase 2 | **ANSWERED** |
| OQ-007 | Ticket | Phase 2 | **ANSWERED** |
| OQ-008 | Channels | Phase 3 | **ANSWERED** |
| OQ-009 | Channels | Phase 3 | **ANSWERED** |
| OQ-010 | Channels | Phase 3 | **ANSWERED** |
| OQ-011 | Channels | Phase 3 | **ANSWERED** |
| OQ-012 | Channels | Phase 3 | **ANSWERED** |
| OQ-013 | Agent | Phase 4 | **ANSWERED** |
| OQ-014 | Agent | Phase 4 | **ANSWERED** |
| OQ-015 | Agent | Phase 4 | **ANSWERED** |
| OQ-016 | SLA | Phase 2 | **ANSWERED** |
| OQ-017 | SLA | Phase 4 | **ANSWERED** |
| OQ-018 | SLA | Phase 3 | **ANSWERED** |
| OQ-019 | SLA | Phase 2 | **ANSWERED** |
| OQ-020 | Platform | Phase 2 | **ANSWERED** |
| OQ-021 | Knowledge Base | Phase 4 | **ANSWERED** |
| OQ-022 | AI | Phase 5 | **ANSWERED** |
| OQ-023 | AI | Phase 5 | **ANSWERED** |
| OQ-024 | AI | Phase 5 | **ANSWERED** |
| OQ-025 | Portal | Phase 3 | **ANSWERED** |
| OQ-026 | Portal | Phase 4 | **ANSWERED** |
| OQ-027 | Reports | Phase 5 | **ANSWERED** |
| OQ-028 | Security | Phase 2 | **ANSWERED** |
| OQ-029 | Security | Phase 2 | **ANSWERED** |
| OQ-030 | Security | Phase 2 | **ANSWERED** |
| OQ-031 | Integrations | Phase 5 | **ANSWERED** |
| OQ-032 | Platform | Phase 2 | **ANSWERED** |
| OQ-033 | Platform | Phase 2 | **ANSWERED** |
| OQ-034 | Platform | Phase 4 | **ANSWERED** |

**Total: 34 | Answered: 34 | Remaining open: 0**
**ALL PHASES FULLY ANSWERED ✅**
**Ready to proceed to Phase 2 — Domain Model & Architecture**
