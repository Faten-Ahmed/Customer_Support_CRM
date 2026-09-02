import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { SlaReportComponent } from './sla-report.component';
import { ReportService, SlaComplianceReport } from '../report.service';

const mockReport: SlaComplianceReport = {
  firstResponseComplianceRate: 90,
  resolutionComplianceRate: 85,
  avgFirstResponseMinutes: 15,
  avgResolutionMinutes: 120,
  byPriority: {
    High: { firstResponseComplianceRate: 80, resolutionComplianceRate: 75, totalTickets: 20 },
  },
  breachReasons: { warningCount: 3, breachCount: 5, criticalBreachCount: 1 },
};

describe('SlaReportComponent', () => {
  let fixture: ComponentFixture<SlaReportComponent>;
  let component: SlaReportComponent;
  let reportService: { getSlaReport: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    reportService = {
      getSlaReport: vi.fn().mockReturnValue(of(mockReport)),
    };

    await TestBed.configureTestingModule({
      imports: [SlaReportComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: ReportService, useValue: reportService }],
    }).compileComponents();

    fixture = TestBed.createComponent(SlaReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load SLA report', () => {
    expect(component).toBeTruthy();
    expect(component.report()).toBeTruthy();
    expect(reportService.getSlaReport).toHaveBeenCalled();
  });

  it('should show error state and retry button on API failure', async () => {
    reportService.getSlaReport.mockReturnValue(throwError(() => new Error('Server error')));
    component.load();
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Retry');
  });

  it('should reload on filter change', () => {
    component.filterForm.patchValue({ dateFrom: new Date('2025-01-01'), dateTo: new Date('2025-01-31') });
    component.applyFilter();
    expect(reportService.getSlaReport).toHaveBeenCalledTimes(2);
  });
});
