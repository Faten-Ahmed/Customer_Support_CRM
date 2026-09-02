import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { CsatReportComponent } from './csat-report.component';
import { CsatReport, ReportService } from '../report.service';

const mockReport: CsatReport = {
  overall: { avgRating: 4.2, totalSent: 150, totalSubmitted: 120, responseRate: 80.0 },
  distribution: { 5: 63, 4: 40 },
  byDepartment: [{ departmentId: '1', departmentName: 'Support', avgRating: 4.1, totalSubmitted: 100 }],
  byAgent: [{ agentId: '1', agentName: 'Alice', avgRating: 4.3, totalSubmitted: 50 }],
  recentComments: ['Great service'],
};

describe('CsatReportComponent', () => {
  let fixture: ComponentFixture<CsatReportComponent>;
  let component: CsatReportComponent;
  let reportService: { getCsatReport: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    reportService = {
      getCsatReport: vi.fn().mockReturnValue(of(mockReport)),
    };

    await TestBed.configureTestingModule({
      imports: [CsatReportComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: ReportService, useValue: reportService }],
    }).compileComponents();

    fixture = TestBed.createComponent(CsatReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load CSAT report', () => {
    expect(component).toBeTruthy();
    expect(component.report()).toBeTruthy();
    expect(reportService.getCsatReport).toHaveBeenCalled();
  });

  it('should show error state and retry button on API failure', async () => {
    reportService.getCsatReport.mockReturnValue(throwError(() => new Error('Server error')));
    component.load();
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Retry');
  });

  it('should reload on filter change', () => {
    component.filterForm.patchValue({ dateFrom: '2025-01-01', dateTo: '2025-01-31' });
    component.applyFilter();
    expect(reportService.getCsatReport).toHaveBeenCalledTimes(2);
  });
});
