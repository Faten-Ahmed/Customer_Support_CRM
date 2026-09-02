import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { SlaIndicatorComponent, slaColour } from './sla-indicator.component';
import { TicketService, SlaStatus } from '../../tickets/ticket.service';

const makeSla = (overrides: Partial<SlaStatus> = {}): SlaStatus => ({
  isPaused: false,
  firstResponse: {
    dueAt: '2026-08-26T12:00:00Z',
    elapsedPercent: 45,
    breached: false,
    remainingLabel: '2h 15m',
  },
  resolution: {
    dueAt: '2026-08-28T12:00:00Z',
    elapsedPercent: 10,
    breached: false,
    remainingLabel: '46h 30m',
  },
  ...overrides,
});

// ─── slaColour() helper ───────────────────────────────────────────────────────

describe('slaColour()', () => {
  it('returns "green" when elapsedPercent < 50', () => {
    expect(slaColour(0)).toBe('green');
    expect(slaColour(49)).toBe('green');
  });

  it('returns "yellow" when elapsedPercent is 50–79', () => {
    expect(slaColour(50)).toBe('yellow');
    expect(slaColour(79)).toBe('yellow');
  });

  it('returns "orange" when elapsedPercent is 80–99', () => {
    expect(slaColour(80)).toBe('orange');
    expect(slaColour(99)).toBe('orange');
  });

  it('returns "red" when elapsedPercent >= 100 (breached)', () => {
    expect(slaColour(100)).toBe('red');
    expect(slaColour(150)).toBe('red');
  });
});

// ─── Badge mode ───────────────────────────────────────────────────────────────

describe('SlaIndicatorComponent — badge mode', () => {
  let fixture: ComponentFixture<SlaIndicatorComponent>;
  let component: SlaIndicatorComponent;
  let getSla: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    getSla = vi.fn().mockReturnValue(of(makeSla()));

    await TestBed.configureTestingModule({
      imports: [SlaIndicatorComponent, NoopAnimationsModule],
      providers: [{ provide: TicketService, useValue: { getSla } }],
    }).compileComponents();

    fixture = TestBed.createComponent(SlaIndicatorComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-77';
    component.mode = 'badge';
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create in badge mode', () => {
    expect(component).toBeTruthy();
  });

  it('should render a coloured dot with green class when < 50%', () => {
    const dot = fixture.debugElement.query(By.css('[data-testid="sla-dot"]'));
    expect(dot).not.toBeNull();
    expect(dot.classes['sla-green']).toBe(true);
  });

  it('should display remaining time label', () => {
    const label = fixture.debugElement.query(By.css('[data-testid="sla-remaining"]'));
    expect(label.nativeElement.textContent.trim()).toBe('2h 15m');
  });

  it('should show red dot when firstResponse is breached (elapsedPercent >= 100)', () => {
    getSla.mockReturnValue(
      of(makeSla({
        firstResponse: {
          dueAt: '2026-08-26T10:00:00Z',
          elapsedPercent: 120,
          breached: true,
          remainingLabel: '-30m',
        },
      }))
    );
    const f2 = TestBed.createComponent(SlaIndicatorComponent);
    f2.componentInstance.ticketId = 'ticket-77';
    f2.componentInstance.mode = 'badge';
    f2.detectChanges();

    const dot = f2.debugElement.query(By.css('[data-testid="sla-dot"]'));
    expect(dot.classes['sla-red']).toBe(true);
  });
});

// ─── Detail mode ──────────────────────────────────────────────────────────────

describe('SlaIndicatorComponent — detail mode', () => {
  let fixture: ComponentFixture<SlaIndicatorComponent>;
  let component: SlaIndicatorComponent;
  let getSla: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    getSla = vi.fn().mockReturnValue(of(makeSla()));

    await TestBed.configureTestingModule({
      imports: [SlaIndicatorComponent, NoopAnimationsModule],
      providers: [{ provide: TicketService, useValue: { getSla } }],
    }).compileComponents();

    fixture = TestBed.createComponent(SlaIndicatorComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-77';
    component.mode = 'detail';
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should render detail panel with both clocks', () => {
    const detail = fixture.debugElement.query(By.css('[data-testid="sla-detail"]'));
    expect(detail).not.toBeNull();
    expect(fixture.debugElement.query(By.css('[data-testid="sla-first-response"]'))).not.toBeNull();
    expect(fixture.debugElement.query(By.css('[data-testid="sla-resolution"]'))).not.toBeNull();
  });

  it('should show "Paused" label when isPaused is true', () => {
    getSla.mockReturnValue(of(makeSla({ isPaused: true })));
    const f2 = TestBed.createComponent(SlaIndicatorComponent);
    f2.componentInstance.ticketId = 'ticket-77';
    f2.componentInstance.mode = 'detail';
    f2.detectChanges();

    const paused = f2.debugElement.query(By.css('[data-testid="sla-paused"]'));
    expect(paused).not.toBeNull();
    expect(paused.nativeElement.textContent.trim()).toBe('Paused');
  });

  it('should show breach text when firstResponse is breached', () => {
    getSla.mockReturnValue(
      of(makeSla({
        firstResponse: {
          dueAt: '2026-08-26T10:00:00Z',
          elapsedPercent: 130,
          breached: true,
          remainingLabel: '-30m',
        },
      }))
    );
    const f2 = TestBed.createComponent(SlaIndicatorComponent);
    f2.componentInstance.ticketId = 'ticket-77';
    f2.componentInstance.mode = 'detail';
    f2.detectChanges();

    const breachText = f2.debugElement.query(By.css('[data-testid="breach-text-fr"]'));
    expect(breachText).not.toBeNull();
    expect(breachText.nativeElement.textContent).toContain('Breached');
  });

  it('should call getSla once on init in detail mode', () => {
    expect(getSla).toHaveBeenCalledWith('ticket-77');
    expect(getSla).toHaveBeenCalledTimes(1);
  });

  it('should poll getSla on init in detail mode (initial load is synchronous)', () => {
    getSla.mockReturnValue(of(makeSla()));
    const f2 = TestBed.createComponent(SlaIndicatorComponent);
    f2.componentInstance.ticketId = 'ticket-77';
    f2.componentInstance.mode = 'detail';
    getSla.mockClear();
    f2.detectChanges();

    // Initial call is synchronous (not timer-based)
    expect(getSla).toHaveBeenCalledWith('ticket-77');
    expect(getSla).toHaveBeenCalledTimes(1);

    f2.destroy();
  });
});
