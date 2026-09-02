import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ManagementDashboardComponent } from './management-dashboard.component';
import { DashboardService, KpiData } from '../dashboard.service';
import { DepartmentService } from '../../admin/departments/department.service';
import { SignalRService } from '../../shared/services/signalr.service';
import { AuthStore } from '../../auth/auth.store';

const mockKpi: KpiData = {
  openTickets: 12, slaBreachRate: 5, avgFirstResponseMinutes7Day: 30, escalationRate: 2,
  unassignedTickets: 4, csatScore30Day: 87, agentUtilization: 72,
  ticketsTodayCreated: 8, ticketsTodayResolved: 6,
};

describe('ManagementDashboardComponent', () => {
  let fixture: ComponentFixture<ManagementDashboardComponent>;
  let component: ManagementDashboardComponent;
  let dashboardService: { getKpis: ReturnType<typeof vi.fn> };
  let signalRService: { getConnection: ReturnType<typeof vi.fn> };
  let mockConnection: { start: ReturnType<typeof vi.fn>; on: ReturnType<typeof vi.fn>; stop: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    mockConnection = {
      start: vi.fn().mockResolvedValue(undefined),
      on: vi.fn(),
      stop: vi.fn(),
    };

    dashboardService = {
      getKpis: vi.fn().mockReturnValue(of(mockKpi)),
    };

    signalRService = {
      getConnection: vi.fn().mockReturnValue(mockConnection),
    };

    await TestBed.configureTestingModule({
      imports: [ManagementDashboardComponent, NoopAnimationsModule],
      providers: [
        { provide: DashboardService, useValue: dashboardService },
        { provide: DepartmentService, useValue: { list: vi.fn().mockReturnValue(of({ data: [] })) } },
        { provide: SignalRService, useValue: signalRService },
        { provide: AuthStore, useValue: { user: () => ({ role: 'Admin' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ManagementDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => component.ngOnDestroy());

  it('should create and display KPI cards', async () => {
    await fixture.whenStable();
    expect(component).toBeTruthy();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('12');
    expect(el.textContent).toContain('Open Tickets');
  });

  it('should connect to DashboardHub via SignalR', () => {
    expect(signalRService.getConnection).toHaveBeenCalledWith('/hubs/dashboard');
    expect(mockConnection.start).toHaveBeenCalled();
  });

  it('should show department filter for Admin role', async () => {
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Department');
  });

  it('should reload KPIs on refresh button click', () => {
    component.refresh();
    expect(dashboardService.getKpis).toHaveBeenCalledTimes(2);
  });
});
