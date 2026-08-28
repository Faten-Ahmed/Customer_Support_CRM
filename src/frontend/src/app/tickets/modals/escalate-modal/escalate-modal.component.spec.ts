import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { EscalateModalComponent } from './escalate-modal.component';
import { TicketService } from '../../ticket.service';

describe('EscalateModalComponent', () => {
  let fixture: ComponentFixture<EscalateModalComponent>;
  let component: EscalateModalComponent;
  const mockTicketService = { escalate: vi.fn() };
  const mockDialogRef = { close: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.escalate.mockReturnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [EscalateModalComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { ticketId: 't1' } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EscalateModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should be invalid when reason is empty', () => {
    expect(component.form.invalid).toBe(true);
  });

  it('should call escalate() and close dialog on submit', () => {
    component.form.get('reason')!.setValue('Customer is VIP and very upset');
    component.onSubmit();
    expect(mockTicketService.escalate).toHaveBeenCalledWith('t1', 'Customer is VIP and very upset');
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });
});
