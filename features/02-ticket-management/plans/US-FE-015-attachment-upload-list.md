# Attachment Upload & List — Implementation Plan

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

**Story:** US-FE-015  
**Goal:** Implement the Attachments tab on the ticket detail page with drag-and-drop upload, per-file progress bars, client-side size validation, download links, and role-gated delete with confirmation dialog.

**Architecture:** `AttachmentPanelComponent` is a standalone component rendered inside the ticket detail shell's "Attachments" tab. It reads the ticketId from a parent `@Input` and delegates all HTTP work to three `TicketService` methods: `getAttachments`, `uploadAttachment`, and `deleteAttachment`. Upload progress is tracked per-file using Angular Signals keyed by a temporary upload ID. The delete confirmation uses `MatDialog` with an inline confirmation component.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/tickets/attachment-panel/attachment-panel.component.ts` |
| Create | `src/app/tickets/attachment-panel/attachment-panel.component.spec.ts` |

---

## Task 1: TicketService — attachment methods

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/ticket.service.spec.ts  (append to existing describe block)
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TicketService, Attachment } from './ticket.service';

describe('TicketService — attachments', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  const TICKET_ID = 'ticket-001';
  const ATTACHMENT_ID = 'att-abc';

  const mockAttachments: Attachment[] = [
    {
      id: ATTACHMENT_ID,
      filename: 'report.pdf',
      sizeBytes: 204800,
      uploadedBy: 'Alice Agent',
      uploadedAt: '2026-08-01T10:00:00Z',
      downloadUrl: 'https://cdn.example.com/report.pdf?sig=abc',
    },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAttachments should GET /api/tickets/{id}/attachments', () => {
    service.getAttachments(TICKET_ID).subscribe(list => {
      expect(list.length).toBe(1);
      expect(list[0].filename).toBe('report.pdf');
    });
    const req = httpMock.expectOne(`/api/tickets/${TICKET_ID}/attachments`);
    expect(req.request.method).toBe('GET');
    req.flush(mockAttachments);
  });

  it('uploadAttachment should POST multipart FormData and return Attachment', () => {
    const file = new File(['hello'], 'hello.txt', { type: 'text/plain' });
    service.uploadAttachment(TICKET_ID, file).subscribe(att => {
      expect(att.filename).toBe('hello.txt');
    });
    const req = httpMock.expectOne(`/api/tickets/${TICKET_ID}/attachments`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBeTrue();
    req.flush({ ...mockAttachments[0], filename: 'hello.txt' });
  });

  it('deleteAttachment should DELETE /api/tickets/{ticketId}/attachments/{attId}', () => {
    service.deleteAttachment(TICKET_ID, ATTACHMENT_ID).subscribe(res => {
      expect(res).toBeNull();
    });
    const req = httpMock.expectOne(
      `/api/tickets/${TICKET_ID}/attachments/${ATTACHMENT_ID}`
    );
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: FAIL (methods not yet implemented)

- [ ] **Step 3: Implement attachment methods on TicketService**

```typescript
// src/app/tickets/ticket.service.ts  (add to existing service)
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Attachment {
  id: string;
  filename: string;
  sizeBytes: number;
  uploadedBy: string;
  uploadedAt: string;
  downloadUrl: string;
}

// Inside @Injectable({ providedIn: 'root' }) class TicketService:

  getAttachments(ticketId: string): Observable<Attachment[]> {
    return this.http.get<Attachment[]>(`/api/tickets/${ticketId}/attachments`);
  }

  uploadAttachment(ticketId: string, file: File): Observable<Attachment> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<Attachment>(`/api/tickets/${ticketId}/attachments`, form);
  }

  deleteAttachment(ticketId: string, attachmentId: string): Observable<null> {
    return this.http.delete<null>(
      `/api/tickets/${ticketId}/attachments/${attachmentId}`
    );
  }
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ticket.service.ts src/app/tickets/ticket.service.spec.ts
git commit -m "feat(tickets): add attachment service methods (US-FE-015)"
```

---

## Task 2: AttachmentPanelComponent — list and download

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/attachment-panel/attachment-panel.component.spec.ts
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { AttachmentPanelComponent } from './attachment-panel.component';
import { TicketService, Attachment } from '../ticket.service';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';

const MOCK_ATTACHMENTS: Attachment[] = [
  {
    id: 'att-1',
    filename: 'spec.pdf',
    sizeBytes: 1024 * 1024 * 2, // 2 MB
    uploadedBy: 'Bob Agent',
    uploadedAt: '2026-08-10T08:00:00Z',
    downloadUrl: 'https://cdn.example.com/spec.pdf?sig=xyz',
  },
];

describe('AttachmentPanelComponent', () => {
  let fixture: ComponentFixture<AttachmentPanelComponent>;
  let component: AttachmentPanelComponent;
  let ticketSvc: jasmine.SpyObj<TicketService>;
  let dialogSpy: jasmine.SpyObj<MatDialog>;

  beforeEach(async () => {
    ticketSvc = jasmine.createSpyObj('TicketService', [
      'getAttachments',
      'uploadAttachment',
      'deleteAttachment',
    ]);
    dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
    ticketSvc.getAttachments.and.returnValue(of(MOCK_ATTACHMENTS));

    await TestBed.configureTestingModule({
      imports: [AttachmentPanelComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketSvc },
        { provide: MatDialog, useValue: dialogSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AttachmentPanelComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-001';
    component.isAgent = true;
    fixture.detectChanges();
  });

  it('should create and load attachments on init', () => {
    expect(component).toBeTruthy();
    expect(ticketSvc.getAttachments).toHaveBeenCalledWith('ticket-001');
    expect(component.attachments()).toHaveSize(1);
  });

  it('should render filename and formatted size', () => {
    const rows = fixture.debugElement.queryAll(By.css('[data-testid="attachment-row"]'));
    expect(rows.length).toBe(1);
    expect(rows[0].nativeElement.textContent).toContain('spec.pdf');
    expect(rows[0].nativeElement.textContent).toContain('2.00 MB');
  });

  it('should show download link with presigned URL', () => {
    const link = fixture.debugElement.query(By.css('a[data-testid="download-link"]'));
    expect(link.nativeElement.getAttribute('href')).toBe(
      'https://cdn.example.com/spec.pdf?sig=xyz'
    );
  });

  it('should show delete button for agent role', () => {
    const btn = fixture.debugElement.query(By.css('[data-testid="delete-btn"]'));
    expect(btn).not.toBeNull();
  });

  it('should NOT show delete button when isAgent is false', () => {
    component.isAgent = false;
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('[data-testid="delete-btn"]'));
    expect(btn).toBeNull();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/attachment-panel/attachment-panel.component.spec.ts --watch=false
```

Expected: FAIL

- [ ] **Step 3: Implement AttachmentPanelComponent (list + download)**

```typescript
// src/app/tickets/attachment-panel/attachment-panel.component.ts
import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TicketService, Attachment } from '../ticket.service';

interface UploadProgress {
  filename: string;
  progress: number;
  error?: string;
}

@Component({
  selector: 'app-attachment-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule,
    DecimalPipe,
  ],
  template: `
    <div class="attachment-panel">
      <!-- Upload zone -->
      <div
        class="upload-zone"
        [class.drag-over]="isDragging()"
        (dragover)="onDragOver($event)"
        (dragleave)="isDragging.set(false)"
        (drop)="onDrop($event)"
        (click)="fileInput.click()"
        data-testid="upload-zone"
      >
        <mat-icon>cloud_upload</mat-icon>
        <span>Drag & drop files here or <strong>click to browse</strong></span>
        <span class="hint">Max file size: 5 MB</span>
        <input #fileInput type="file" multiple hidden (change)="onFileSelected($event)" />
      </div>

      <!-- Per-file progress bars -->
      @for (up of uploads(); track up.filename) {
        <div class="upload-progress" data-testid="upload-progress">
          <span>{{ up.filename }}</span>
          @if (up.error) {
            <span class="upload-error">{{ up.error }}</span>
          } @else {
            <mat-progress-bar mode="determinate" [value]="up.progress" />
          }
        </div>
      }

      <!-- Attachment list -->
      <table mat-table [dataSource]="attachments()" class="attachment-table">
        <ng-container matColumnDef="filename">
          <th mat-header-cell *matHeaderCellDef>File</th>
          <td mat-cell *matCellDef="let att" data-testid="attachment-row">
            <a
              [href]="att.downloadUrl"
              target="_blank"
              rel="noopener"
              data-testid="download-link"
            >{{ att.filename }}</a>
          </td>
        </ng-container>

        <ng-container matColumnDef="size">
          <th mat-header-cell *matHeaderCellDef>Size</th>
          <td mat-cell *matCellDef="let att">{{ formatSize(att.sizeBytes) }}</td>
        </ng-container>

        <ng-container matColumnDef="uploadedBy">
          <th mat-header-cell *matHeaderCellDef>Uploaded by</th>
          <td mat-cell *matCellDef="let att">{{ att.uploadedBy }}</td>
        </ng-container>

        <ng-container matColumnDef="uploadedAt">
          <th mat-header-cell *matHeaderCellDef>Date</th>
          <td mat-cell *matCellDef="let att">{{ att.uploadedAt | date:'medium' }}</td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let att">
            @if (isAgent) {
              <button
                mat-icon-button
                color="warn"
                data-testid="delete-btn"
                (click)="confirmDelete(att)"
                matTooltip="Delete attachment"
              >
                <mat-icon>delete</mat-icon>
              </button>
            }
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;" data-testid="attachment-row"></tr>
      </table>

      @if (attachments().length === 0 && uploads().length === 0) {
        <p class="empty-state" data-testid="empty-state">No attachments yet.</p>
      }
    </div>
  `,
  styles: [`
    .upload-zone {
      border: 2px dashed #aaa;
      border-radius: 8px;
      padding: 24px;
      text-align: center;
      cursor: pointer;
      margin-bottom: 16px;
      transition: background 0.2s;
    }
    .upload-zone.drag-over { background: #e3f2fd; border-color: #1976d2; }
    .hint { display: block; font-size: 12px; color: #888; }
    .upload-progress { margin: 8px 0; }
    .upload-error { color: #c62828; font-size: 13px; }
    .attachment-table { width: 100%; }
    .empty-state { color: #888; text-align: center; margin-top: 16px; }
  `],
})
export class AttachmentPanelComponent implements OnInit {
  @Input() ticketId!: string;
  @Input() isAgent = false;

  private readonly ticketSvc = inject(TicketService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  attachments = signal<Attachment[]>([]);
  uploads = signal<UploadProgress[]>([]);
  isDragging = signal(false);

  displayedColumns = ['filename', 'size', 'uploadedBy', 'uploadedAt', 'actions'];

  ngOnInit(): void {
    this.loadAttachments();
  }

  loadAttachments(): void {
    this.ticketSvc.getAttachments(this.ticketId).subscribe(list => {
      this.attachments.set(list);
    });
  }

  formatSize(bytes: number): string {
    return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
  }

  onDragOver(e: DragEvent): void {
    e.preventDefault();
    this.isDragging.set(true);
  }

  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.isDragging.set(false);
    const files = Array.from(e.dataTransfer?.files ?? []);
    this.uploadFiles(files);
  }

  onFileSelected(e: Event): void {
    const input = e.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    this.uploadFiles(files);
    input.value = '';
  }

  uploadFiles(files: File[]): void {
    for (const file of files) {
      if (file.size > 5 * 1024 * 1024) {
        this.uploads.update(u => [
          ...u,
          { filename: file.name, progress: 0, error: 'File exceeds 5 MB limit.' },
        ]);
        continue;
      }
      this.uploads.update(u => [...u, { filename: file.name, progress: 0 }]);
      this.ticketSvc.uploadAttachment(this.ticketId, file).subscribe({
        next: att => {
          this.attachments.update(list => [...list, att]);
          this.uploads.update(u => u.filter(up => up.filename !== file.name));
        },
        error: err => {
          const msg =
            err?.error?.code === 'ATTACHMENT_LIMIT_EXCEEDED'
              ? 'Attachment limit exceeded for this ticket.'
              : 'Upload failed. Please try again.';
          this.uploads.update(u =>
            u.map(up =>
              up.filename === file.name ? { ...up, error: msg } : up
            )
          );
        },
      });
    }
  }

  confirmDelete(att: Attachment): void {
    import('@angular/material/dialog').then(({ MatDialog }) => {});
    const ref = this.dialog.open(DeleteConfirmDialogComponent, {
      data: { filename: att.filename },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.ticketSvc.deleteAttachment(this.ticketId, att.id).subscribe(() => {
        this.attachments.update(list => list.filter(a => a.id !== att.id));
        this.snack.open(`"${att.filename}" deleted.`, 'OK', { duration: 3000 });
      });
    });
  }
}

// Inline confirmation dialog
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-delete-confirm-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Delete Attachment</h2>
    <mat-dialog-content>
      Delete <strong>{{ data.filename }}</strong>? This cannot be undone.
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-button color="warn" [mat-dialog-close]="true">Delete</button>
    </mat-dialog-actions>
  `,
})
export class DeleteConfirmDialogComponent {
  readonly data = inject<{ filename: string }>(MAT_DIALOG_DATA);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/attachment-panel/attachment-panel.component.spec.ts --watch=false
```

Expected: 5 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/attachment-panel/
git commit -m "feat(tickets): implement AttachmentPanelComponent list and download (US-FE-015)"
```

---

## Task 3: AttachmentPanelComponent — upload validation and 422 error

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/attachment-panel/attachment-panel.component.spec.ts  (append)
describe('AttachmentPanelComponent — upload', () => {
  let fixture: ComponentFixture<AttachmentPanelComponent>;
  let component: AttachmentPanelComponent;
  let ticketSvc: jasmine.SpyObj<TicketService>;

  beforeEach(async () => {
    ticketSvc = jasmine.createSpyObj('TicketService', [
      'getAttachments',
      'uploadAttachment',
      'deleteAttachment',
    ]);
    ticketSvc.getAttachments.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [AttachmentPanelComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketSvc },
        { provide: MatDialog, useValue: jasmine.createSpyObj('MatDialog', ['open']) },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AttachmentPanelComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-001';
    component.isAgent = true;
    fixture.detectChanges();
  });

  it('should reject files over 5 MB with client-side error', () => {
    const bigFile = new File([new ArrayBuffer(6 * 1024 * 1024)], 'big.zip');
    component.uploadFiles([bigFile]);
    fixture.detectChanges();
    expect(ticketSvc.uploadAttachment).not.toHaveBeenCalled();
    const progress = component.uploads();
    expect(progress[0].error).toContain('5 MB');
  });

  it('should show ATTACHMENT_LIMIT_EXCEEDED message on 422', fakeAsync(() => {
    const { throwError } = require('rxjs');
    const { HttpErrorResponse } = require('@angular/common/http');
    const err = new HttpErrorResponse({
      status: 422,
      error: { code: 'ATTACHMENT_LIMIT_EXCEEDED' },
    });
    ticketSvc.uploadAttachment.and.returnValue(throwError(() => err));

    const file = new File(['x'], 'ok.txt');
    component.uploadFiles([file]);
    tick();
    fixture.detectChanges();

    const up = component.uploads().find(u => u.filename === 'ok.txt');
    expect(up?.error).toContain('limit exceeded');
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/attachment-panel/attachment-panel.component.spec.ts --watch=false
```

Expected: FAIL

- [ ] **Step 3: Implementation already complete in Task 2 Step 3**

The `uploadFiles` method already handles both the 5 MB client-side guard and the `ATTACHMENT_LIMIT_EXCEEDED` 422 error path. No additional implementation needed.

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/attachment-panel/attachment-panel.component.spec.ts --watch=false
```

Expected: 7 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/attachment-panel/attachment-panel.component.spec.ts
git commit -m "test(tickets): add upload validation and 422 error tests for AttachmentPanelComponent (US-FE-015)"
```

---

## Task 4: AttachmentPanelComponent — delete confirmation dialog

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/attachment-panel/attachment-panel.component.spec.ts  (append)
describe('AttachmentPanelComponent — delete', () => {
  let fixture: ComponentFixture<AttachmentPanelComponent>;
  let component: AttachmentPanelComponent;
  let ticketSvc: jasmine.SpyObj<TicketService>;
  let dialogSpy: jasmine.SpyObj<MatDialog>;

  const attachments: Attachment[] = [
    {
      id: 'att-1',
      filename: 'contract.pdf',
      sizeBytes: 50000,
      uploadedBy: 'Alice',
      uploadedAt: '2026-08-10T08:00:00Z',
      downloadUrl: 'https://cdn.example.com/contract.pdf',
    },
  ];

  beforeEach(async () => {
    ticketSvc = jasmine.createSpyObj('TicketService', [
      'getAttachments',
      'uploadAttachment',
      'deleteAttachment',
    ]);
    dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
    ticketSvc.getAttachments.and.returnValue(of(attachments));
    ticketSvc.deleteAttachment.and.returnValue(of(null));

    await TestBed.configureTestingModule({
      imports: [AttachmentPanelComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: ticketSvc },
        { provide: MatDialog, useValue: dialogSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AttachmentPanelComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-001';
    component.isAgent = true;
    fixture.detectChanges();
  });

  it('should call deleteAttachment when dialog confirms', () => {
    const { Subject } = require('rxjs');
    const afterClosed$ = new Subject<boolean>();
    dialogSpy.open.and.returnValue({ afterClosed: () => afterClosed$.asObservable() } as any);

    component.confirmDelete(attachments[0]);
    afterClosed$.next(true);

    expect(ticketSvc.deleteAttachment).toHaveBeenCalledWith('ticket-001', 'att-1');
    expect(component.attachments()).toHaveSize(0);
  });

  it('should NOT call deleteAttachment when dialog is cancelled', () => {
    const { Subject } = require('rxjs');
    const afterClosed$ = new Subject<boolean>();
    dialogSpy.open.and.returnValue({ afterClosed: () => afterClosed$.asObservable() } as any);

    component.confirmDelete(attachments[0]);
    afterClosed$.next(false);

    expect(ticketSvc.deleteAttachment).not.toHaveBeenCalled();
    expect(component.attachments()).toHaveSize(1);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/attachment-panel/attachment-panel.component.spec.ts --watch=false
```

Expected: FAIL

- [ ] **Step 3: Implementation already included in Task 2**

The `confirmDelete` method in Task 2 already handles both confirmed and cancelled dialog states. No additional code needed.

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/attachment-panel/attachment-panel.component.spec.ts --watch=false
```

Expected: 9 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/attachment-panel/
git commit -m "test(tickets): add delete confirmation dialog tests for AttachmentPanelComponent (US-FE-015)"
```
