import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { DepartmentListComponent } from './department-list.component';
import { DepartmentService, Department } from './department.service';

const mockDepartments: Department[] = [
  { id: 'd1', name: 'Support', isActive: true, createdAt: '2025-01-01T00:00:00Z' },
  { id: 'd2', name: 'Sales', isActive: false, createdAt: '2025-01-02T00:00:00Z' },
];

describe('DepartmentListComponent', () => {
  let fixture: ComponentFixture<DepartmentListComponent>;
  let component: DepartmentListComponent;
  let deptService: {
    list: ReturnType<typeof vi.fn>;
    deactivate: ReturnType<typeof vi.fn>;
    reactivate: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    deptService = {
      list: vi.fn().mockReturnValue(of({ data: mockDepartments })),
      deactivate: vi.fn().mockReturnValue(of({})),
      reactivate: vi.fn().mockReturnValue(of({})),
    };

    await TestBed.configureTestingModule({
      imports: [DepartmentListComponent, NoopAnimationsModule],
      providers: [
        { provide: DepartmentService, useValue: deptService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DepartmentListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load departments on init', () => {
    expect(deptService.list).toHaveBeenCalled();
    expect(component.departments().length).toBe(2);
  });

  it('should open dialog for new department', () => {
    const dialog = fixture.debugElement.injector.get(MatDialog);
    vi.spyOn(dialog, 'open').mockReturnValue({ afterClosed: () => of(null) } as any);
    component.openNewDepartmentDialog();
    expect(dialog.open).toHaveBeenCalled();
  });

  it('should deactivate a department', () => {
    component.deactivate(mockDepartments[0]);
    expect(deptService.deactivate).toHaveBeenCalledWith('d1');
  });

  it('should reactivate a department', () => {
    component.reactivate(mockDepartments[1]);
    expect(deptService.reactivate).toHaveBeenCalledWith('d2');
  });
});
