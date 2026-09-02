import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { AssignModalComponent } from './assign-modal.component';
import { TicketService } from '../../ticket.service';

describe('AssignModalComponent', () => {
  let fixture: ComponentFixture<AssignModalComponent>;
  let component: AssignModalComponent;
  const mockTicketService = { assign: vi.fn() };
  const mockDialogRef = { close: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.assign.mockReturnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [AssignModalComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { ticketId: 't1', agents: [{ id: 'a1', name: 'Omar' }] } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AssignModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should be invalid when no agent selected', () => {
    expect(component.form.invalid).toBe(true);
  });

  it('should call assign() and close dialog on submit', () => {
    component.form.get('agentId')!.setValue('a1');
    component.onSubmit();
    expect(mockTicketService.assign).toHaveBeenCalledWith('t1', 'a1');
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });
});
