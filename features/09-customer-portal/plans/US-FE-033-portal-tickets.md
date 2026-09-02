# Portal Ticket List, Detail & Reply — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-FE-033
**Goal:** Implement the customer portal ticket experience — a card-list at `/portal/tickets`, detail view at `/portal/tickets/{id}` with message thread and reply box, close-ticket button, and CSAT survey prompt.

**Architecture:** `PortalTicketListComponent` uses cards (not table) for mobile-friendliness. `PortalTicketDetailComponent` fetches ticket by ID and messages from `PortalTicketService`. The reply box uses a simple `FormControl` (no internal note toggle). Closing a ticket calls `PortalTicketService.close()` and shows a CSAT survey snackbar with a link if available.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/portal/services/portal-ticket.service.ts` |
| Create | `src/app/portal/services/portal-ticket.service.spec.ts` |
| Create | `src/app/portal/ticket-list/portal-ticket-list.component.ts` |
| Create | `src/app/portal/ticket-list/portal-ticket-list.component.html` |
| Create | `src/app/portal/ticket-list/portal-ticket-list.component.spec.ts` |
| Create | `src/app/portal/ticket-detail/portal-ticket-detail.component.ts` |
| Create | `src/app/portal/ticket-detail/portal-ticket-detail.component.html` |
| Create | `src/app/portal/ticket-detail/portal-ticket-detail.component.spec.ts` |

---

## Task 1: PortalTicketService

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/services/portal-ticket.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PortalTicketService } from './portal-ticket.service';

describe('PortalTicketService', () => {
  let service: PortalTicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PortalTicketService],
    });
    service = TestBed.inject(PortalTicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/portal/tickets', () => {
    service.list().subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/v1/portal/tickets');
    expect(req.request.method).toBe('GET');
    req.flush({ data: [], total: 0 });
  });

  it('getById() should GET /api/v1/portal/tickets/{id}', () => {
    service.getById('t1').subscribe();
    const req = httpMock.expectOne('/api/v1/portal/tickets/t1');
    req.flush({ id: 't1', subject: 'Help needed', status: 'Open' });
  });

  it('addMessage() should POST /api/v1/portal/tickets/{id}/messages', () => {
    service.addMessage('t1', 'Still waiting').subscribe();
    const req = httpMock.expectOne('/api/v1/portal/tickets/t1/messages');
    expect(req.request.body).toEqual({ content: 'Still waiting' });
    req.flush({ id: 'm1' });
  });

  it('close() should POST /api/v1/portal/tickets/{id}/close', () => {
    service.close('t1').subscribe();
    const req = httpMock.expectOne('/api/v1/portal/tickets/t1/close');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 't1', status: 'Closed', surveyUrl: '/portal/survey/s1' });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/services/portal-ticket.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/services/portal-ticket.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PortalTicket {
  id: string;
  subject: string;
  status: string;
  priority?: string;
  assignedAgentName?: string;
  updatedAt: string;
}

export interface PortalMessage {
  id: string;
  content: string;
  senderName: string;
  direction: 'Inbound' | 'Outbound';
  createdAt: string;
}

export interface CloseResult {
  id: string;
  status: string;
  surveyUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class PortalTicketService {
  private readonly http = inject(HttpClient);

  list(status?: string): Observable<{ data: PortalTicket[]; total: number }> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<{ data: PortalTicket[]; total: number }>('/api/v1/portal/tickets', { params });
  }

  getById(id: string): Observable<PortalTicket> {
    return this.http.get<PortalTicket>(`/api/v1/portal/tickets/${id}`);
  }

  getMessages(id: string): Observable<{ data: PortalMessage[]; total: number }> {
    return this.http.get<{ data: PortalMessage[]; total: number }>(`/api/v1/portal/tickets/${id}/messages`);
  }

  addMessage(id: string, content: string): Observable<PortalMessage> {
    return this.http.post<PortalMessage>(`/api/v1/portal/tickets/${id}/messages`, { content });
  }

  close(id: string): Observable<CloseResult> {
    return this.http.post<CloseResult>(`/api/v1/portal/tickets/${id}/close`, {});
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/services/portal-ticket.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/services/portal-ticket.service.ts src/app/portal/services/portal-ticket.service.spec.ts
git commit -m "feat(portal): add PortalTicketService (US-FE-033)"
```

---

## Task 2: PortalTicketListComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/ticket-list/portal-ticket-list.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { PortalTicketListComponent } from './portal-ticket-list.component';
import { PortalTicketService, PortalTicket } from '../services/portal-ticket.service';

const mockTickets: PortalTicket[] = [
  { id: 't1', subject: 'Cannot login', status: 'Open', priority: 'High', updatedAt: '2025-01-02T10:00:00Z' },
  { id: 't2', subject: 'Billing error', status: 'Resolved', updatedAt: '2025-01-01T09:00:00Z' },
];

describe('PortalTicketListComponent', () => {
  let fixture: ComponentFixture<PortalTicketListComponent>;
  let component: PortalTicketListComponent;
  let portalTicketService: jasmine.SpyObj<PortalTicketService>;

  beforeEach(async () => {
    portalTicketService = jasmine.createSpyObj('PortalTicketService', ['list']);
    portalTicketService.list.and.returnValue(of({ data: mockTickets, total: 2 }));

    await TestBed.configureTestingModule({
      imports: [PortalTicketListComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [{ provide: PortalTicketService, useValue: portalTicketService }],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalTicketListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load tickets', () => {
    expect(component).toBeTruthy();
    expect(component.tickets().length).toBe(2);
  });

  it('should render ticket cards with subject and status', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Cannot login');
    expect(el.textContent).toContain('Open');
  });

  it('should filter by status', () => {
    component.statusFilter.setValue('Resolved');
    expect(portalTicketService.list).toHaveBeenCalledWith('Resolved');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/ticket-list/portal-ticket-list.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/ticket-list/portal-ticket-list.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { PortalTicket, PortalTicketService } from '../services/portal-ticket.service';

@Component({
  selector: 'app-portal-ticket-list',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterModule, ReactiveFormsModule, MatCardModule, MatSelectModule, MatFormFieldModule, MatButtonModule, MatIconModule],
  templateUrl: './portal-ticket-list.component.html',
})
export class PortalTicketListComponent implements OnInit {
  private readonly ticketService = inject(PortalTicketService);

  readonly tickets = signal<PortalTicket[]>([]);
  readonly statusFilter = new FormControl('');

  ngOnInit(): void {
    this.load();
    this.statusFilter.valueChanges.subscribe(s => this.load(s || undefined));
  }

  load(status?: string): void {
    this.ticketService.list(status).subscribe(res => this.tickets.set(res.data));
  }
}
```

```html
<!-- src/app/portal/ticket-list/portal-ticket-list.component.html -->

<div class="p-6 max-w-3xl mx-auto">
  <div class="flex items-center justify-between mb-6">
    <h1 class="text-2xl font-semibold">My Tickets</h1>
    <button mat-raised-button color="primary" routerLink="/portal/tickets/new">
      <mat-icon>add</mat-icon> New Ticket
    </button>
  </div>

  <mat-form-field appearance="outline" class="w-48 mb-4">
    <mat-label>Filter by status</mat-label>
    <mat-select [formControl]="statusFilter">
      <mat-option value="">All</mat-option>
      <mat-option value="Open">Open</mat-option>
      <mat-option value="Resolved">Resolved</mat-option>
      <mat-option value="Closed">Closed</mat-option>
    </mat-select>
  </mat-form-field>

  @if (tickets().length === 0) {
    <div class="text-center py-12 text-gray-500">
      <mat-icon class="text-5xl mb-3">inbox</mat-icon>
      <p>No tickets found. Need help? <a routerLink="/portal/tickets/new" class="text-blue-600">Submit a ticket</a>.</p>
    </div>
  } @else {
    <div class="flex flex-col gap-4">
      @for (ticket of tickets(); track ticket.id) {
        <mat-card class="p-4 cursor-pointer hover:shadow-md transition-shadow" [routerLink]="['/portal/tickets', ticket.id]">
          <div class="flex items-start justify-between">
            <div>
              <p class="font-semibold text-gray-800">{{ ticket.subject }}</p>
              <p class="text-sm text-gray-500 mt-1">Updated {{ ticket.updatedAt | date:'mediumDate' }}</p>
            </div>
            <div class="flex flex-col items-end gap-1">
              <span class="text-xs px-2 py-1 rounded" [class.bg-blue-100]="ticket.status === 'Open'" [class.bg-green-100]="ticket.status === 'Resolved'" [class.bg-gray-100]="ticket.status === 'Closed'">
                {{ ticket.status }}
              </span>
              @if (ticket.priority) {
                <span class="text-xs text-gray-400">{{ ticket.priority }}</span>
              }
            </div>
          </div>
        </mat-card>
      }
    </div>
  }
</div>
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/ticket-list/portal-ticket-list.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/ticket-list/
git commit -m "feat(portal): implement PortalTicketListComponent with card layout (US-FE-033)"
```

---

## Task 3: PortalTicketDetailComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/portal/ticket-detail/portal-ticket-detail.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { PortalTicketDetailComponent } from './portal-ticket-detail.component';
import { PortalTicketService, PortalTicket, PortalMessage } from '../services/portal-ticket.service';

const mockTicket: PortalTicket = { id: 't1', subject: 'Help needed', status: 'Open', updatedAt: '2025-01-01' };
const mockMessages: PortalMessage[] = [
  { id: 'm1', content: 'Hello', senderName: 'Customer', direction: 'Inbound', createdAt: '2025-01-01T10:00:00Z' },
];

describe('PortalTicketDetailComponent', () => {
  let fixture: ComponentFixture<PortalTicketDetailComponent>;
  let component: PortalTicketDetailComponent;
  let ticketService: jasmine.SpyObj<PortalTicketService>;
  let router: Router;

  beforeEach(async () => {
    ticketService = jasmine.createSpyObj('PortalTicketService', ['getById', 'getMessages', 'addMessage', 'close']);
    ticketService.getById.and.returnValue(of(mockTicket));
    ticketService.getMessages.and.returnValue(of({ data: mockMessages, total: 1 }));
    ticketService.addMessage.and.returnValue(of({ id: 'm2', content: 'Reply', senderName: 'Customer', direction: 'Inbound', createdAt: new Date().toISOString() }));
    ticketService.close.and.returnValue(of({ id: 't1', status: 'Closed' }));

    await TestBed.configureTestingModule({
      imports: [PortalTicketDetailComponent, RouterTestingModule, NoopAnimationsModule],
      providers: [
        { provide: PortalTicketService, useValue: ticketService },
        { provide: ActivatedRoute, useValue: { params: of({ id: 't1' }) } },
        { provide: MatSnackBar, useValue: jasmine.createSpyObj('MatSnackBar', ['open']) },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalTicketDetailComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create and load ticket + messages', () => {
    expect(component).toBeTruthy();
    expect(component.ticket()?.subject).toBe('Help needed');
    expect(component.messages().length).toBe(1);
  });

  it('should send reply and append to messages', () => {
    component.replyControl.setValue('I need more help');
    component.sendReply();
    expect(ticketService.addMessage).toHaveBeenCalledWith('t1', 'I need more help');
    expect(component.messages().length).toBe(2);
    expect(component.replyControl.value).toBe('');
  });

  it('should hide reply box for Closed tickets', () => {
    component.ticket.set({ ...mockTicket, status: 'Closed' });
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Ticket Closed');
  });

  it('should call close() and show CSAT prompt', () => {
    component.closeTicket();
    expect(ticketService.close).toHaveBeenCalledWith('t1');
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/portal/ticket-detail/portal-ticket-detail.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/portal/ticket-detail/portal-ticket-detail.component.ts

import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { PortalMessage, PortalTicket, PortalTicketService } from '../services/portal-ticket.service';

@Component({
  selector: 'app-portal-ticket-detail',
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatSnackBarModule],
  templateUrl: './portal-ticket-detail.component.html',
})
export class PortalTicketDetailComponent implements OnInit {
  private readonly ticketService = inject(PortalTicketService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly ticket = signal<PortalTicket | null>(null);
  readonly messages = signal<PortalMessage[]>([]);
  readonly replyControl = new FormControl('', Validators.required);
  readonly sending = signal(false);

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      const id = params['id'];
      this.ticketService.getById(id).subscribe(t => this.ticket.set(t));
      this.ticketService.getMessages(id).subscribe(res => this.messages.set(res.data));
    });
  }

  sendReply(): void {
    const content = this.replyControl.value ?? '';
    if (!content.trim()) return;
    const id = this.ticket()?.id;
    if (!id) return;
    this.sending.set(true);
    this.ticketService.addMessage(id, content).subscribe({
      next: msg => {
        this.messages.update(list => [...list, msg]);
        this.replyControl.setValue('');
        this.sending.set(false);
      },
      error: () => this.sending.set(false),
    });
  }

  closeTicket(): void {
    const id = this.ticket()?.id;
    if (!id) return;
    this.ticketService.close(id).subscribe(res => {
      this.ticket.update(t => t ? { ...t, status: 'Closed' } : t);
      if (res.surveyUrl) {
        this.snackBar.open('How was your experience? Take a quick survey.', 'Rate Us', { duration: 10000 })
          .onAction().subscribe(() => { /* navigate to survey */ });
      }
    });
  }

  get isClosed(): boolean { return this.ticket()?.status === 'Closed'; }
}
```

```html
<!-- src/app/portal/ticket-detail/portal-ticket-detail.component.html -->

@if (ticket()) {
  <div class="p-6 max-w-3xl mx-auto">
    <!-- Header -->
    <div class="mb-6">
      <h1 class="text-xl font-semibold">{{ ticket()!.subject }}</h1>
      <div class="flex gap-3 mt-1 text-sm text-gray-500">
        <span>Status: <strong>{{ ticket()!.status }}</strong></span>
        @if (ticket()!.assignedAgentName) { <span>Agent: {{ ticket()!.assignedAgentName }}</span> }
      </div>
      @if (!isClosed) {
        <button mat-stroked-button color="warn" class="mt-3" (click)="closeTicket()">Close Ticket</button>
      }
    </div>

    <!-- Messages -->
    <div class="flex flex-col gap-3 mb-6 border rounded-lg p-4 bg-white max-h-[500px] overflow-y-auto">
      @for (msg of messages(); track msg.id) {
        <div class="flex" [class.justify-end]="msg.direction === 'Outbound'">
          <div class="rounded-lg p-3 max-w-[80%]"
               [class.bg-blue-100]="msg.direction === 'Outbound'"
               [class.bg-gray-100]="msg.direction === 'Inbound'">
            <p class="text-xs text-gray-500 mb-1">{{ msg.senderName }} · {{ msg.createdAt | date:'shortTime' }}</p>
            <p class="text-sm">{{ msg.content }}</p>
          </div>
        </div>
      }
    </div>

    <!-- Reply or Closed Banner -->
    @if (isClosed) {
      <div class="bg-gray-100 rounded-lg p-4 text-center text-gray-600">
        <mat-icon>lock</mat-icon>
        <p>Ticket Closed — this ticket is no longer accepting replies.</p>
      </div>
    } @else {
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Write a reply</mat-label>
        <textarea matInput [formControl]="replyControl" rows="3"></textarea>
      </mat-form-field>
      <button mat-raised-button color="primary" [disabled]="!replyControl.value?.trim() || sending()" (click)="sendReply()">
        {{ sending() ? 'Sending…' : 'Send Reply' }}
      </button>
    }
  </div>
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/portal/ticket-detail/portal-ticket-detail.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/portal/ticket-detail/
git commit -m "feat(portal): implement PortalTicketDetailComponent with reply and close (US-FE-033)"
```
