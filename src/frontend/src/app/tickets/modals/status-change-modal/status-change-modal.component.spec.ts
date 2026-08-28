import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { StatusChangeModalComponent } from './status-change-modal.component';
import { TicketService } from '../../ticket.service';

describe('StatusChangeModalComponent', () => {
  let fixture: ComponentFixture<StatusChangeModalComponent>;
  let component: StatusChangeModalComponent;
  const mockTicketService = { changeStatus: vi.fn() };
  const mockDialogRef = { close: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.changeStatus.mockReturnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [StatusChangeModalComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        {
          provide: MAT_DIALOG_DATA,
          useValue: { ticketId: 't1', availableStatuses: ['OnHold', 'Resolved'] },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StatusChangeModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should be invalid when no status selected', () => {
    expect(component.form.invalid).toBe(true);
  });

  it('should call changeStatus() and close dialog on submit', () => {
    component.form.get('status')!.setValue('OnHold');
    component.onSubmit();
    expect(mockTicketService.changeStatus).toHaveBeenCalledWith('t1', 'OnHold', undefined);
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });
});
