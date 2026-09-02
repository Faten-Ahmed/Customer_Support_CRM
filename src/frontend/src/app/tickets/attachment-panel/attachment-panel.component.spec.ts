import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AttachmentPanelComponent } from './attachment-panel.component';
import { TicketService, Attachment } from '../ticket.service';
import { MatDialog } from '@angular/material/dialog';
import { of, Subject, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';

const MOCK_ATTACHMENTS: Attachment[] = [
  {
    id: 'att-1',
    ticketId: 'ticket-001',
    fileName: 'spec.pdf',
    contentType: 'application/pdf',
    fileSize: 1024 * 1024 * 2,
    uploaderName: 'Bob Agent',
    uploadedAt: '2026-08-10T08:00:00Z',
    presignedUrl: 'https://cdn.example.com/spec.pdf?sig=xyz',
  },
];

describe('AttachmentPanelComponent — list and download', () => {
  let fixture: ComponentFixture<AttachmentPanelComponent>;
  let component: AttachmentPanelComponent;
  const mockTicketService = {
    getAttachments: vi.fn(),
    uploadAttachment: vi.fn(),
    deleteAttachment: vi.fn(),
  };
  const mockDialog = { open: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.getAttachments.mockReturnValue(of(MOCK_ATTACHMENTS));

    await TestBed.configureTestingModule({
      imports: [AttachmentPanelComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: MatDialog, useValue: mockDialog },
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
    expect(mockTicketService.getAttachments).toHaveBeenCalledWith('ticket-001');
    expect(component.attachments()).toHaveLength(1);
  });

  it('should render fileName and formatted size', () => {
    const rows = fixture.debugElement.queryAll(By.css('[data-testid="attachment-row"]'));
    expect(rows.length).toBe(1);
    expect(rows[0].nativeElement.textContent).toContain('spec.pdf');
    expect(rows[0].nativeElement.textContent).toContain('2.00 MB');
  });

  it('should show download link with presigned URL', () => {
    const link = fixture.debugElement.query(By.css('a[data-testid="download-link"]'));
    expect(link.nativeElement.getAttribute('href')).toBe('https://cdn.example.com/spec.pdf?sig=xyz');
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

describe('AttachmentPanelComponent — upload', () => {
  let fixture: ComponentFixture<AttachmentPanelComponent>;
  let component: AttachmentPanelComponent;
  const mockTicketService = {
    getAttachments: vi.fn(),
    uploadAttachment: vi.fn(),
    deleteAttachment: vi.fn(),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.getAttachments.mockReturnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [AttachmentPanelComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: MatDialog, useValue: { open: vi.fn() } },
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
    expect(mockTicketService.uploadAttachment).not.toHaveBeenCalled();
    expect(component.uploads()[0].error).toContain('5 MB');
  });

  it('should show ATTACHMENT_LIMIT_EXCEEDED message on 422 error', async () => {
    const err = new HttpErrorResponse({
      status: 422,
      error: { code: 'ATTACHMENT_LIMIT_EXCEEDED' },
    });
    mockTicketService.uploadAttachment.mockReturnValue(throwError(() => err));

    const file = new File(['x'], 'ok.txt');
    component.uploadFiles([file]);

    await fixture.whenStable();
    fixture.detectChanges();

    const up = component.uploads().find(u => u.filename === 'ok.txt');
    expect(up?.error).toContain('limit exceeded');
  });
});

describe('AttachmentPanelComponent — delete', () => {
  let fixture: ComponentFixture<AttachmentPanelComponent>;
  let component: AttachmentPanelComponent;
  const mockTicketService = {
    getAttachments: vi.fn(),
    uploadAttachment: vi.fn(),
    deleteAttachment: vi.fn(),
  };
  const mockDialog = { open: vi.fn() };

  const attachments: Attachment[] = [
    {
      id: 'att-1',
      ticketId: 'ticket-001',
      fileName: 'contract.pdf',
      contentType: 'application/pdf',
      fileSize: 50000,
      uploaderName: 'Alice',
      uploadedAt: '2026-08-10T08:00:00Z',
      presignedUrl: 'https://cdn.example.com/contract.pdf',
    },
  ];

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.getAttachments.mockReturnValue(of(attachments));
    mockTicketService.deleteAttachment.mockReturnValue(of(null));

    await TestBed.configureTestingModule({
      imports: [AttachmentPanelComponent, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: MatDialog, useValue: mockDialog },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AttachmentPanelComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-001';
    component.isAgent = true;
    fixture.detectChanges();
  });

  it('should call deleteAttachment when dialog confirms', () => {
    const afterClosed$ = new Subject<boolean>();
    mockDialog.open.mockReturnValue({ afterClosed: () => afterClosed$.asObservable() });

    component.confirmDelete(attachments[0]);
    afterClosed$.next(true);

    expect(mockTicketService.deleteAttachment).toHaveBeenCalledWith('ticket-001', 'att-1');
    expect(component.attachments()).toHaveLength(0);
  });

  it('should NOT call deleteAttachment when dialog is cancelled', () => {
    const afterClosed$ = new Subject<boolean>();
    mockDialog.open.mockReturnValue({ afterClosed: () => afterClosed$.asObservable() });

    component.confirmDelete(attachments[0]);
    afterClosed$.next(false);

    expect(mockTicketService.deleteAttachment).not.toHaveBeenCalled();
    expect(component.attachments()).toHaveLength(1);
  });
});
