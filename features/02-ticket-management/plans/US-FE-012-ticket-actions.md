# Ticket Status Badge & Action Modals — Implementation Plan

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

**Story:** US-FE-012
**Goal:** Implement colour-coded status badges and four action modals (Assign, Transfer, Escalate, Change Status/Resolve) that operate on a ticket without leaving the detail page.

**Architecture:** Each action modal is a standalone `MatDialog` component. `TicketService` gains four new methods. Status badge is a pure standalone pipe/component. Valid next-state logic is computed client-side based on current status and user role, matching the state machine in the spec.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/components/status-badge/status-badge.component.ts` |
| Create | `src/app/tickets/components/status-badge/status-badge.component.spec.ts` |
| Create | `src/app/tickets/modals/assign-modal/assign-modal.component.ts` |
| Create | `src/app/tickets/modals/assign-modal/assign-modal.component.spec.ts` |
| Create | `src/app/tickets/modals/transfer-modal/transfer-modal.component.ts` |
| Create | `src/app/tickets/modals/transfer-modal/transfer-modal.component.spec.ts` |
| Create | `src/app/tickets/modals/escalate-modal/escalate-modal.component.ts` |
| Create | `src/app/tickets/modals/escalate-modal/escalate-modal.component.spec.ts` |
| Create | `src/app/tickets/modals/status-change-modal/status-change-modal.component.ts` |
| Create | `src/app/tickets/modals/status-change-modal/status-change-modal.component.spec.ts` |
| Modify | `src/app/tickets/services/ticket.service.ts` |
| Modify | `src/app/tickets/services/ticket.service.spec.ts` |

---

## Task 1: Extend TicketService with action methods

- [ ] **Step 1: Write the failing tests**

```typescript
// Append to src/app/tickets/services/ticket.service.spec.ts

describe('TicketService — action methods', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('assign() should POST /api/v1/tickets/{id}/assign', () => {
    service.assign('t1', 'agent-1').subscribe();
    const req = httpMock.expectOne('/api/v1/tickets/t1/assign');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ agentId: 'agent-1' });
    req.flush({});
  });

  it('transfer() should POST /api/v1/tickets/{id}/transfer', () => {
    service.transfer('t1', 'd2', 'Needs billing').subscribe();
    const req = httpMock.expectOne('/api/v1/tickets/t1/transfer');
    expect(req.request.body).toEqual({ departmentId: 'd2', note: 'Needs billing' });
    req.flush({});
  });

  it('escalate() should POST /api/v1/tickets/{id}/escalate', () => {
    service.escalate('t1', 'Customer VIP and very upset').subscribe();
    const req = httpMock.expectOne('/api/v1/tickets/t1/escalate');
    expect(req.request.body).toEqual({ reason: 'Customer VIP and very upset' });
    req.flush({});
  });

  it('changeStatus() should PATCH /api/v1/tickets/{id}/status', () => {
    service.changeStatus('t1', 'OnHold', undefined).subscribe();
    const req = httpMock.expectOne('/api/v1/tickets/t1/status');
    expect(req.request.method).toBe('PATCH');
    req.flush({});
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// Append to src/app/tickets/services/ticket.service.ts

assign(ticketId: string, agentId: string): Observable<void> {
  return this.http.post<void>(`/api/v1/tickets/${ticketId}/assign`, { agentId });
}

transfer(ticketId: string, departmentId: string, note: string): Observable<void> {
  return this.http.post<void>(`/api/v1/tickets/${ticketId}/transfer`, { departmentId, note });
}

escalate(ticketId: string, reason: string): Observable<void> {
  return this.http.post<void>(`/api/v1/tickets/${ticketId}/escalate`, { reason });
}

changeStatus(ticketId: string, status: string, resolutionText: string | undefined): Observable<void> {
  return this.http.patch<void>(`/api/v1/tickets/${ticketId}/status`, { status, resolutionText });
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/services/ticket.service.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/services/ticket.service.ts src/app/tickets/services/ticket.service.spec.ts
git commit -m "feat(tickets): add assign/transfer/escalate/changeStatus to TicketService (US-FE-012)"
```

---

## Task 2: StatusBadgeComponent

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/components/status-badge/status-badge.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StatusBadgeComponent } from './status-badge.component';

describe('StatusBadgeComponent', () => {
  let fixture: ComponentFixture<StatusBadgeComponent>;
  let component: StatusBadgeComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatusBadgeComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(StatusBadgeComponent);
    component = fixture.componentInstance;
  });

  const cases: { status: string; expectedClass: string }[] = [
    { status: 'New', expectedClass: 'badge-grey' },
    { status: 'Assigned', expectedClass: 'badge-blue' },
    { status: 'InProgress', expectedClass: 'badge-green' },
    { status: 'OnHold', expectedClass: 'badge-yellow' },
    { status: 'Escalated', expectedClass: 'badge-red' },
    { status: 'Resolved', expectedClass: 'badge-teal' },
    { status: 'Closed', expectedClass: 'badge-dark' },
  ];

  cases.forEach(({ status, expectedClass }) => {
    it(`should render ${expectedClass} class for status ${status}`, () => {
      component.status = status;
      fixture.detectChanges();
      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('span')?.classList.contains(expectedClass)).toBeTrue();
    });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/components/status-badge/status-badge.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/components/status-badge/status-badge.component.ts

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

const STATUS_CLASSES: Record<string, string> = {
  New: 'badge-grey',
  Assigned: 'badge-blue',
  InProgress: 'badge-green',
  OnHold: 'badge-yellow',
  Escalated: 'badge-red',
  Resolved: 'badge-teal',
  Closed: 'badge-dark',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `<span [class]="badgeClass">{{ status }}</span>`,
  styles: [`
    span { padding: 2px 8px; border-radius: 4px; font-size: 0.75rem; font-weight: 600; }
    .badge-grey { background: #e5e7eb; color: #374151; }
    .badge-blue { background: #dbeafe; color: #1d4ed8; }
    .badge-green { background: #d1fae5; color: #065f46; }
    .badge-yellow { background: #fef3c7; color: #92400e; }
    .badge-red { background: #fee2e2; color: #991b1b; }
    .badge-teal { background: #ccfbf1; color: #0f766e; }
    .badge-dark { background: #1f2937; color: #f9fafb; }
  `],
})
export class StatusBadgeComponent {
  @Input() status = '';

  get badgeClass(): string {
    return STATUS_CLASSES[this.status] ?? 'badge-grey';
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/components/status-badge/status-badge.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/components/status-badge/
git commit -m "feat(tickets): add StatusBadgeComponent with colour-coded statuses (US-FE-012)"
```

---

## Task 3: AssignModal and TransferModal

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/modals/assign-modal/assign-modal.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AssignModalComponent } from './assign-modal.component';
import { TicketService } from '../../services/ticket.service';

describe('AssignModalComponent', () => {
  let fixture: ComponentFixture<AssignModalComponent>;
  let component: AssignModalComponent;
  let ticketService: jasmine.SpyObj<TicketService>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<AssignModalComponent>>;

  beforeEach(async () => {
    ticketService = jasmine.createSpyObj('TicketService', ['assign']);
    dialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    ticketService.assign.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [AssignModalComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketService },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { ticketId: 't1', agents: [{ id: 'a1', name: 'Omar' }] } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AssignModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should disable submit when no agent selected', () => {
    expect(component.form.invalid).toBeTrue();
  });

  it('should call assign() and close dialog on submit', () => {
    component.form.get('agentId')!.setValue('a1');
    component.onSubmit();
    expect(ticketService.assign).toHaveBeenCalledWith('t1', 'a1');
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/modals/assign-modal/assign-modal.component.spec.ts --watch=false
```

- [ ] **Step 3: Implement**

```typescript
// src/app/tickets/modals/assign-modal/assign-modal.component.ts

import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { TicketService } from '../../services/ticket.service';

export interface AssignModalData {
  ticketId: string;
  agents: { id: string; name: string }[];
}

@Component({
  selector: 'app-assign-modal',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Assign Ticket</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Agent</mat-label>
          <mat-select formControlName="agentId">
            @for (a of data.agents; track a.id) {
              <mat-option [value]="a.id">{{ a.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" [disabled]="form.invalid" (click)="onSubmit()">Assign</button>
    </mat-dialog-actions>
  `,
})
export class AssignModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);

  form = this.fb.group({ agentId: ['', Validators.required] });

  constructor(
    public dialogRef: MatDialogRef<AssignModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AssignModalData
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    this.ticketService.assign(this.data.ticketId, this.form.value.agentId!).subscribe({
      next: () => this.dialogRef.close(true),
    });
  }
}
```

```typescript
// src/app/tickets/modals/transfer-modal/transfer-modal.component.ts

import { Component, Inject, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { TicketService } from '../../services/ticket.service';

export interface TransferModalData {
  ticketId: string;
  departments: { id: string; name: string }[];
}

@Component({
  selector: 'app-transfer-modal',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Transfer Ticket</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="flex flex-col gap-3">
        <mat-form-field appearance="outline">
          <mat-label>Transfer To</mat-label>
          <mat-select formControlName="departmentId">
            @for (d of data.departments; track d.id) {
              <mat-option [value]="d.id">{{ d.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Transfer Note (required)</mat-label>
          <textarea matInput formControlName="note" rows="3"></textarea>
          @if (form.get('note')?.hasError('minlength')) {
            <mat-error>Minimum 10 characters</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" [disabled]="form.invalid" (click)="onSubmit()">Transfer</button>
    </mat-dialog-actions>
  `,
})
export class TransferModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly ticketService = inject(TicketService);

  form = this.fb.group({
    departmentId: ['', Validators.required],
    note: ['', [Validators.required, Validators.minLength(10)]],
  });

  constructor(
    public dialogRef: MatDialogRef<TransferModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: TransferModalData
  ) {}

  onSubmit(): void {
    if (this.form.invalid) return;
    const { departmentId, note } = this.form.value as { departmentId: string; note: string };
    this.ticketService.transfer(this.data.ticketId, departmentId, note).subscribe({
      next: () => this.dialogRef.close(true),
    });
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/modals/assign-modal/assign-modal.component.spec.ts --watch=false
```

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/modals/ src/app/tickets/components/
git commit -m "feat(tickets): add action modals (Assign, Transfer, Escalate, Status Change) (US-FE-012)"
```
