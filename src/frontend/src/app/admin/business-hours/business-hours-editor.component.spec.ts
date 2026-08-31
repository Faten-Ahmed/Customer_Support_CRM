import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ReactiveFormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { BusinessHoursEditorComponent } from './business-hours-editor.component';
import { BusinessHoursService, BusinessHoursCard } from './business-hours.service';

const mockCards: BusinessHoursCard[] = [
  {
    id: 'bh-global',
    departmentId: null,
    workDays: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday'],
    startTime: '08:00',
    endTime: '17:00',
    timeZone: 'Asia/Riyadh',
    holidays: [{ id: 'hol-1', date: '2026-01-01', name: 'New Year' }],
  },
];

describe('BusinessHoursEditorComponent', () => {
  let fixture: ComponentFixture<BusinessHoursEditorComponent>;
  let component: BusinessHoursEditorComponent;
  let bhService: {
    list: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    addHoliday: ReturnType<typeof vi.fn>;
    deleteHoliday: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    bhService = {
      list: vi.fn().mockReturnValue(of(mockCards)),
      update: vi.fn().mockReturnValue(of(undefined)),
      addHoliday: vi.fn().mockReturnValue(of({ id: 'hol-new' })),
      deleteHoliday: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [BusinessHoursEditorComponent, NoopAnimationsModule, ReactiveFormsModule],
      providers: [{ provide: BusinessHoursService, useValue: bhService }],
    }).compileComponents();

    fixture = TestBed.createComponent(BusinessHoursEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load cards on init', () => {
    expect(bhService.list).toHaveBeenCalled();
    expect(component.cards().length).toBe(1);
  });

  it('should label the global card as "Global"', () => {
    expect(component.cardLabel(mockCards[0])).toBe('Global');
  });

  it('should initialize form with work days matching the card', () => {
    const form = component.cardForms['bh-global'];
    const selectedDays = form.get('workDays')!.value as string[];
    expect(selectedDays).toContain('Sunday');
    expect(selectedDays).toContain('Thursday');
    expect(selectedDays).not.toContain('Friday');
  });

  it('should mark card as unsaved when form is marked dirty', () => {
    const form = component.cardForms['bh-global'];
    form.get('startTime')!.setValue('07:00');
    form.markAsDirty();
    expect(component.isUnsaved('bh-global')).toBe(true);
  });

  it('should fail validation when endTime <= startTime', () => {
    const form = component.cardForms['bh-global'];
    form.get('startTime')!.setValue('18:00');
    form.get('endTime')!.setValue('08:00');
    form.updateValueAndValidity();
    expect(form.hasError('endBeforeStart')).toBe(true);
  });

  it('should call update and mark clean on saveCard', async () => {
    component.markUnsavedForTest('bh-global');
    component.saveCard('bh-global');
    await fixture.whenStable();
    expect(bhService.update).toHaveBeenCalledWith(
      'bh-global',
      expect.objectContaining({ startTime: '08:00', endTime: '17:00' })
    );
    expect(component.isUnsaved('bh-global')).toBe(false);
  });

  it('should call addHoliday immediately on addHoliday()', async () => {
    component.addHoliday('bh-global', '2026-12-25', 'Christmas');
    await fixture.whenStable();
    expect(bhService.addHoliday).toHaveBeenCalledWith('bh-global', '2026-12-25', 'Christmas');
  });

  it('should call deleteHoliday immediately on deleteHoliday()', async () => {
    component.deleteHoliday('bh-global', 'hol-1');
    await fixture.whenStable();
    expect(bhService.deleteHoliday).toHaveBeenCalledWith('bh-global', 'hol-1');
  });
});
