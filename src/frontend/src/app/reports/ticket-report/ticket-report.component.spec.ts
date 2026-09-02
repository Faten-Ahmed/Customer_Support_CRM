import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { TicketReportComponent } from './ticket-report.component';
import { ReportService, TicketVolumeReport } from '../report.service';

const mockReport: TicketVolumeReport = {
  summary: { totalCreated: 120, totalResolved: 80, totalClosed: 30, openAtEndOfPeriod: 10 },
  byStatus: { New: 10, InProgress: 20, Resolved: 80, Closed: 30 },
  byPriority: { High: 30, Medium: 60, Low: 30 },
  byChannel: { Email: 70, Portal: 50 },
  trend: [{ date: '2025-01-01', created: 5, resolved: 3 }],
};

describe('TicketReportComponent', () => {
  let fixture: ComponentFixture<TicketReportComponent>;
  let component: TicketReportComponent;
  let reportService: { getTicketReport: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    reportService = {
      getTicketReport: vi.fn().mockReturnValue(of(mockReport)),
    };

    await TestBed.configureTestingModule({
      imports: [TicketReportComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: ReportService, useValue: reportService }],
    }).compileComponents();

    fixture = TestBed.createComponent(TicketReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load report', () => {
    expect(component).toBeTruthy();
    expect(component.report()).toBeTruthy();
    expect(reportService.getTicketReport).toHaveBeenCalled();
  });

  it('should show error state and retry button on API failure', async () => {
    reportService.getTicketReport.mockReturnValue(throwError(() => new Error('Server error')));
    component.load();
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Retry');
  });

  it('should reload on filter change', () => {
    component.filterForm.patchValue({ dateFrom: new Date('2025-01-01'), dateTo: new Date('2025-01-31') });
    component.applyFilter();
    expect(reportService.getTicketReport).toHaveBeenCalledTimes(2);
  });
});
