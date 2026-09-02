import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { TransferModalComponent } from './transfer-modal.component';
import { TicketService } from '../../ticket.service';
import { DepartmentService } from '../../../admin/departments/department.service';

describe('TransferModalComponent', () => {
  let fixture: ComponentFixture<TransferModalComponent>;
  let component: TransferModalComponent;
  const mockTicketService = { transfer: vi.fn() };
  const mockDialogRef = { close: vi.fn() };
  const mockDeptService = { list: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.transfer.mockReturnValue(of(undefined));
    mockDeptService.list.mockReturnValue(of({ data: [{ id: 'd2', name: 'Billing', isActive: true }] }));

    await TestBed.configureTestingModule({
      imports: [TransferModalComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: TicketService, useValue: mockTicketService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { ticketId: 't1' } },
        { provide: DepartmentService, useValue: mockDeptService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TransferModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should be invalid when fields are empty', () => {
    expect(component.form.invalid).toBe(true);
  });

  it('should call transfer() with departmentId and transferNote and close dialog on submit', () => {
    component.form.get('departmentId')!.setValue('d2');
    component.form.get('transferNote')!.setValue('Needs billing team attention');
    component.onSubmit();
    expect(mockTicketService.transfer).toHaveBeenCalledWith('t1', 'd2', 'Needs billing team attention');
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });
});
