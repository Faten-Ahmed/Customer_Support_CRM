import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ReplyComposerComponent } from './reply-composer.component';
import { TicketService, TicketMessage } from '../../ticket.service';
import { TemplateService } from '../../template.service';

const sentMessage: TicketMessage = {
  id: 'm10',
  ticketId: 't1',
  body: 'Reply content',
  isInternal: false,
  authorUserId: 'agent-1',
  authorName: 'Agent',
  createdAt: new Date().toISOString(),
};

describe('ReplyComposerComponent', () => {
  let fixture: ComponentFixture<ReplyComposerComponent>;
  let component: ReplyComposerComponent;

  const mockTicketService = { addMessage: vi.fn() };
  const mockTemplateService = { list: vi.fn(), render: vi.fn() };
  const mockDialog = { open: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.addMessage.mockReturnValue(of(sentMessage));
    mockTemplateService.list.mockReturnValue(of({ items: [], totalCount: 0 }));

    await TestBed.configureTestingModule({
      imports: [ReplyComposerComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: TemplateService, useValue: mockTemplateService },
        { provide: MatDialog, useValue: mockDialog },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReplyComposerComponent);
    component = fixture.componentInstance;
    component.ticketId = 't1';
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should disable send button when textarea is empty', () => {
    const btn = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
  });

  it('should toggle isInternal flag', () => {
    expect(component.isInternal()).toBe(false);
    component.toggleInternal();
    expect(component.isInternal()).toBe(true);
  });

  it('should emit messageSent and clear textarea after send', () => {
    let emitted: TicketMessage | null = null;
    component.messageSent.subscribe(m => (emitted = m));
    component.replyControl.setValue('Reply content');
    component.send();
    expect(mockTicketService.addMessage).toHaveBeenCalledWith('t1', 'Reply content', false);
    expect(emitted).toBeTruthy();
    expect(component.replyControl.value).toBe('');
  });

  it('should show character count', () => {
    component.replyControl.setValue('Hello');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('5');
  });
});
