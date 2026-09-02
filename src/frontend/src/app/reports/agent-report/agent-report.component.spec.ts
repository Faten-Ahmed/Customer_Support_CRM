import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { AgentReportComponent } from './agent-report.component';
import { AgentPerformanceRow, ReportService } from '../report.service';

const mockReport: AgentPerformanceRow[] = [
  {
    agentId: 'a1', agentName: 'Alice', ticketsHandled: 45, ticketsResolved: 40,
    avgFirstResponseMinutes: 15, avgResolutionMinutes: 120, slaComplianceRate: 93,
    csatScore: 4.2, csatResponseCount: 10, escalationRate: 2,
  },
];

describe('AgentReportComponent', () => {
  let fixture: ComponentFixture<AgentReportComponent>;
  let component: AgentReportComponent;
  let reportService: { getAgentReport: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    reportService = {
      getAgentReport: vi.fn().mockReturnValue(of(mockReport)),
    };

    await TestBed.configureTestingModule({
      imports: [AgentReportComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: ReportService, useValue: reportService }],
    }).compileComponents();

    fixture = TestBed.createComponent(AgentReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load agent report', () => {
    expect(component).toBeTruthy();
    expect(component.report()).toBeTruthy();
    expect(reportService.getAgentReport).toHaveBeenCalled();
  });

  it('should show error state and retry button on API failure', async () => {
    reportService.getAgentReport.mockReturnValue(throwError(() => new Error('Server error')));
    component.load();
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Retry');
  });

  it('should reload on filter change', () => {
    component.filterForm.patchValue({ dateFrom: new Date('2025-01-01'), dateTo: new Date('2025-01-31') });
    component.applyFilter();
    expect(reportService.getAgentReport).toHaveBeenCalledTimes(2);
  });
});
