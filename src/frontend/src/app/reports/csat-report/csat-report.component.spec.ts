import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { CsatReportComponent } from './csat-report.component';
import { CsatReport, ReportService } from '../report.service';

const mockReport: CsatReport = {
  avgRating: 4.2,
  distribution: [{ rating: 5, count: 63 }, { rating: 4, count: 40 }],
  byDepartment: [{ department: 'Support', avg: 4.1 }],
  comments: [{ content: 'Great service', rating: 5, agentName: 'Alice', date: '2025-01-15' }],
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
