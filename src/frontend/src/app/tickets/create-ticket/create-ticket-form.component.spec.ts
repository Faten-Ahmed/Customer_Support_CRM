import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { CreateTicketFormComponent } from './create-ticket-form.component';
import { TicketService } from '../ticket.service';
import { FieldDefinitionService } from '../field-definition.service';
import { CustomerService } from '../../customers/services/customer.service';
import { DepartmentService } from '../../admin/departments/department.service';
import { CategoryService } from '../../admin/categories/category.service';

describe('CreateTicketFormComponent', () => {
  let fixture: ComponentFixture<CreateTicketFormComponent>;
  let component: CreateTicketFormComponent;

  const mockTicketService = { create: vi.fn() };
  const mockFieldDefService = { list: vi.fn() };
  const mockCustomerService = { list: vi.fn() };
  const mockDepartmentService = { list: vi.fn() };
  const mockCategoryService = { list: vi.fn() };
  const mockRouter = { navigate: vi.fn() };

  const emptyCustomerPage = { items: [], meta: { page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockTicketService.create.mockReturnValue(of({ id: 'new-t', subject: 'S' }));
    mockFieldDefService.list.mockReturnValue(of([]));
    mockCustomerService.list.mockReturnValue(of(emptyCustomerPage));
    mockDepartmentService.list.mockReturnValue(of({ data: [] }));
    mockCategoryService.list.mockReturnValue(of({ data: [] }));

    await TestBed.configureTestingModule({
      imports: [CreateTicketFormComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: TicketService, useValue: mockTicketService },
        { provide: FieldDefinitionService, useValue: mockFieldDefService },
        { provide: CustomerService, useValue: mockCustomerService },
        { provide: DepartmentService, useValue: mockDepartmentService },
        { provide: CategoryService, useValue: mockCategoryService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateTicketFormComponent);
    component = fixture.componentInstance;
    (component as any).router = mockRouter;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should be invalid when required fields are empty', () => {
    expect(component.form.valid).toBe(false);
  });

  it('should reload custom fields when departmentId changes', () => {
    mockFieldDefService.list.mockReturnValue(
      of([{ id: 'f1', label: 'Account #', type: 'text', required: true, options: [] }])
    );
    component.form.get('departmentId')!.setValue('d1');
    expect(mockFieldDefService.list).toHaveBeenCalledWith('d1');
    expect(component.customFieldDefs.length).toBe(1);
  });

  it('should call create() and navigate to ticket on submit', () => {
    component.form.patchValue({
      customerId: 'c1',
      departmentId: 'd1',
      subject: 'Need help',
      subjectAr: 'مساعدة',
      description: 'Details here',
      descriptionAr: 'تفاصيل',
      priority: 'Medium',
    });
    component.onSubmit();
    expect(mockTicketService.create).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/tickets', 'new-t']);
  });
});
