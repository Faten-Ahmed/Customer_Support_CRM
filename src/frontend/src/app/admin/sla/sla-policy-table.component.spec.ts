import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ReactiveFormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { SlaPolicyTableComponent } from './sla-policy-table.component';
import { SlaPolicyService, SlaPolicy } from './sla-policy.service';

const mockPolicies: SlaPolicy[] = [
  {
    id: 'pol-1',
    departmentId: null,
    priority: 'Critical',
    firstResponseMinutes: 15,
    resolutionMinutes: 240,
    warningThresholdPercent: 80,
    breachThresholdPercent: 100,
    criticalBreachThresholdPercent: 200,
  },
  {
    id: 'pol-2',
    departmentId: null,
    priority: 'High',
    firstResponseMinutes: 120,
    resolutionMinutes: 480,
    warningThresholdPercent: 80,
    breachThresholdPercent: 100,
    criticalBreachThresholdPercent: 200,
  },
];

describe('SlaPolicyTableComponent', () => {
  let fixture: ComponentFixture<SlaPolicyTableComponent>;
  let component: SlaPolicyTableComponent;
  let policyService: {
    list: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    policyService = {
      list: vi.fn().mockReturnValue(of(mockPolicies)),
      update: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [SlaPolicyTableComponent, NoopAnimationsModule, ReactiveFormsModule],
      providers: [{ provide: SlaPolicyService, useValue: policyService }],
    }).compileComponents();

    fixture = TestBed.createComponent(SlaPolicyTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load policies on init', () => {
    expect(policyService.list).toHaveBeenCalled();
    expect(component.policies().length).toBe(2);
  });

  it('should group policies by priority', () => {
    const grouped = component.policiesByPriority();
    expect(grouped['Critical'].length).toBe(1);
    expect(grouped['High'].length).toBe(1);
  });

  it('should enter edit mode when startEdit is called', () => {
    component.startEdit(mockPolicies[0]);
    expect(component.editingId()).toBe('pol-1');
    expect(component.editForm.get('firstResponseMinutes')?.value).toBe(15);
    expect(component.editForm.get('resolutionMinutes')?.value).toBe(240);
  });

  it('should cancel edit and clear editingId', () => {
    component.startEdit(mockPolicies[0]);
    component.cancelEdit();
    expect(component.editingId()).toBeNull();
  });

  it('should fail validation when resolutionMinutes < firstResponseMinutes', () => {
    component.startEdit(mockPolicies[0]);
    component.editForm.get('firstResponseMinutes')!.setValue(300);
    component.editForm.get('resolutionMinutes')!.setValue(100);
    component.editForm.updateValueAndValidity();
    expect(component.editForm.hasError('resolutionBeforeResponse')).toBe(true);
  });

  it('should call update and reload on saveEdit with valid form', async () => {
    policyService.list.mockReturnValue(of(mockPolicies));
    component.startEdit(mockPolicies[0]);
    component.editForm.get('firstResponseMinutes')!.setValue(20);
    component.editForm.get('resolutionMinutes')!.setValue(300);
    component.editForm.get('warningThresholdPercent')!.setValue(75);
    component.editForm.get('breachThresholdPercent')!.setValue(100);
    component.editForm.get('criticalBreachThresholdPercent')!.setValue(200);
    component.saveEdit();
    await fixture.whenStable();
    expect(policyService.update).toHaveBeenCalledWith('pol-1', {
      firstResponseMinutes: 20,
      resolutionMinutes: 300,
      warningThresholdPercent: 75,
      breachThresholdPercent: 100,
      criticalBreachThresholdPercent: 200,
    });
    expect(component.editingId()).toBeNull();
  });
});
