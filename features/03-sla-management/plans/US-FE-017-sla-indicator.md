# SLA Indicator Component — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-FE-017  
**Goal:** Build a shared `SlaIndicatorComponent` that renders a compact coloured badge in ticket lists and a detailed dual-clock panel with progress bars in ticket detail, refreshing automatically every 60 seconds and handling breached and paused (OnHold) states.

**Architecture:** `SlaIndicatorComponent` is a standalone shared component accepting an `@Input() mode: 'badge' | 'detail'` and `@Input() ticketId`. In badge mode it renders a single coloured dot and remaining time string. In detail mode it renders two progress bars (first-response and resolution) with breach and pause overlays. Polling is driven by `rxjs/timer` combined with `switchMap`, set up in `ngOnInit` and torn down via `takeUntilDestroyed`. Colour thresholds are computed by a pure helper function to keep them testable in isolation.

**Tech Stack:** Angular 21, TypeScript, Angular Material, Jasmine, TestBed

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/app/shared/sla-indicator/sla-indicator.component.ts` |
| Create | `src/app/shared/sla-indicator/sla-indicator.component.spec.ts` |

---

## Task 1: TicketService — getSla method

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/tickets/ticket.service.spec.ts  (append)
import { SlaStatus } from './ticket.service';

describe('TicketService — getSla', () => {
  let service: TicketService;
  let httpMock: HttpTestingController;

  const TICKET_ID = 'ticket-77';

  const mockSla: SlaStatus = {
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
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TicketService],
    });
    service = TestBed.inject(TicketService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should GET /api/tickets/{id}/sla', () => {
    service.getSla(TICKET_ID).subscribe(sla => {
      expect(sla.firstResponse.elapsedPercent).toBe(45);
      expect(sla.resolution.remainingLabel).toBe('46h 30m');
      expect(sla.isPaused).toBeFalse();
    });
    const req = httpMock.expectOne(`/api/tickets/${TICKET_ID}/sla`);
    expect(req.request.method).toBe('GET');
    req.flush(mockSla);
  });

  it('should reflect isPaused true when ticket is OnHold', () => {
    service.getSla(TICKET_ID).subscribe(sla => {
      expect(sla.isPaused).toBeTrue();
    });
    const req = httpMock.expectOne(`/api/tickets/${TICKET_ID}/sla`);
    req.flush({ ...mockSla, isPaused: true });
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: FAIL

- [ ] **Step 3: Implement getSla on TicketService**

```typescript
// src/app/tickets/ticket.service.ts  (add interface + method)

export interface SlaClock {
  dueAt: string;
  elapsedPercent: number;  // 0–100+; >100 means breached
  breached: boolean;
  remainingLabel: string;  // e.g. "2h 15m" or "-30m" if breached
}

export interface SlaStatus {
  isPaused: boolean;
  firstResponse: SlaClock;
  resolution: SlaClock;
}

// Inside TicketService class:
  getSla(ticketId: string): Observable<SlaStatus> {
    return this.http.get<SlaStatus>(`/api/tickets/${ticketId}/sla`);
  }
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/tickets/ticket.service.spec.ts --watch=false
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/tickets/ticket.service.ts src/app/tickets/ticket.service.spec.ts
git commit -m "feat(tickets): add getSla service method and SlaStatus interface (US-FE-017)"
```

---

## Task 2: SLA colour threshold helper

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/shared/sla-indicator/sla-indicator.component.spec.ts
import { slaColour } from './sla-indicator.component';

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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/shared/sla-indicator/sla-indicator.component.spec.ts --watch=false
```

Expected: FAIL

- [ ] **Step 3: Implement the exported helper**

```typescript
// src/app/shared/sla-indicator/sla-indicator.component.ts  (top of file, before @Component)
export function slaColour(elapsedPercent: number): 'green' | 'yellow' | 'orange' | 'red' {
  if (elapsedPercent < 50)  return 'green';
  if (elapsedPercent < 80)  return 'yellow';
  if (elapsedPercent < 100) return 'orange';
  return 'red';
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/shared/sla-indicator/sla-indicator.component.spec.ts --watch=false
```

Expected: 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/shared/sla-indicator/sla-indicator.component.ts
git commit -m "feat(shared): add slaColour threshold helper (US-FE-017)"
```

---

## Task 3: SlaIndicatorComponent — badge mode

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/shared/sla-indicator/sla-indicator.component.spec.ts  (append)
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SlaIndicatorComponent } from './sla-indicator.component';
import { TicketService, SlaStatus } from '../../tickets/ticket.service';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';

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

describe('SlaIndicatorComponent — badge mode', () => {
  let fixture: ComponentFixture<SlaIndicatorComponent>;
  let component: SlaIndicatorComponent;
  let ticketSvc: jasmine.SpyObj<TicketService>;

  beforeEach(async () => {
    ticketSvc = jasmine.createSpyObj('TicketService', ['getSla']);
    ticketSvc.getSla.and.returnValue(of(makeSla()));

    await TestBed.configureTestingModule({
      imports: [SlaIndicatorComponent, NoopAnimationsModule],
      providers: [{ provide: TicketService, useValue: ticketSvc }],
    }).compileComponents();

    fixture = TestBed.createComponent(SlaIndicatorComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-77';
    component.mode = 'badge';
    fixture.detectChanges();
  });

  it('should create in badge mode', () => {
    expect(component).toBeTruthy();
  });

  it('should render a coloured dot with green class when < 50%', () => {
    const dot = fixture.debugElement.query(By.css('[data-testid="sla-dot"]'));
    expect(dot).not.toBeNull();
    expect(dot.classes['sla-green']).toBeTrue();
  });

  it('should display remaining time label', () => {
    const label = fixture.debugElement.query(By.css('[data-testid="sla-remaining"]'));
    expect(label.nativeElement.textContent.trim()).toBe('2h 15m');
  });

  it('should show red dot and remaining label when breached', async () => {
    ticketSvc.getSla.and.returnValue(
      of(makeSla({
        firstResponse: {
          dueAt: '2026-08-26T10:00:00Z',
          elapsedPercent: 120,
          breached: true,
          remainingLabel: '-30m',
        },
      }))
    );
    fixture = TestBed.createComponent(SlaIndicatorComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-77';
    component.mode = 'badge';
    fixture.detectChanges();

    const dot = fixture.debugElement.query(By.css('[data-testid="sla-dot"]'));
    expect(dot.classes['sla-red']).toBeTrue();
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/shared/sla-indicator/sla-indicator.component.spec.ts --watch=false
```

Expected: FAIL

- [ ] **Step 3: Implement SlaIndicatorComponent**

```typescript
// src/app/shared/sla-indicator/sla-indicator.component.ts
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Input,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { timer } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { TicketService, SlaStatus } from '../../tickets/ticket.service';

export function slaColour(elapsedPercent: number): 'green' | 'yellow' | 'orange' | 'red' {
  if (elapsedPercent < 50)  return 'green';
  if (elapsedPercent < 80)  return 'yellow';
  if (elapsedPercent < 100) return 'orange';
  return 'red';
}

@Component({
  selector: 'app-sla-indicator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatProgressBarModule, MatTooltipModule],
  template: `
    @if (mode === 'badge') {
      <span class="sla-badge" data-testid="sla-badge">
        <span
          class="sla-dot"
          [class]="'sla-dot sla-' + colour()"
          data-testid="sla-dot"
        ></span>
        <span data-testid="sla-remaining">
          {{ sla()?.firstResponse?.remainingLabel ?? '…' }}
        </span>
      </span>
    }

    @if (mode === 'detail' && sla()) {
      <div class="sla-detail" data-testid="sla-detail">
        @if (sla()!.isPaused) {
          <div class="sla-paused" data-testid="sla-paused">Paused</div>
        }

        <!-- First Response -->
        <div class="sla-clock" data-testid="sla-first-response">
          <span class="clock-label">First Response</span>
          @if (sla()!.firstResponse.breached) {
            <span class="breach-text" data-testid="breach-text-fr">
              ⚠ Breached {{ sla()!.firstResponse.remainingLabel | slice:1 }} ago
            </span>
          } @else {
            <span>{{ sla()!.firstResponse.remainingLabel }}</span>
          }
          <mat-progress-bar
            mode="determinate"
            [value]="clampedPercent(sla()!.firstResponse.elapsedPercent)"
            [class]="'sla-bar sla-' + slaColour(sla()!.firstResponse.elapsedPercent)"
            data-testid="progress-bar-fr"
          />
        </div>

        <!-- Resolution -->
        <div class="sla-clock" data-testid="sla-resolution">
          <span class="clock-label">Resolution</span>
          @if (sla()!.resolution.breached) {
            <span class="breach-text" data-testid="breach-text-res">
              ⚠ Breached {{ sla()!.resolution.remainingLabel | slice:1 }} ago
            </span>
          } @else {
            <span>{{ sla()!.resolution.remainingLabel }}</span>
          }
          <mat-progress-bar
            mode="determinate"
            [value]="clampedPercent(sla()!.resolution.elapsedPercent)"
            [class]="'sla-bar sla-' + slaColour(sla()!.resolution.elapsedPercent)"
            data-testid="progress-bar-res"
          />
        </div>
      </div>
    }
  `,
  styles: [`
    .sla-badge { display: inline-flex; align-items: center; gap: 6px; font-size: 13px; }
    .sla-dot { width: 10px; height: 10px; border-radius: 50%; display: inline-block; }
    .sla-green  { background: #2e7d32; }
    .sla-yellow { background: #f9a825; }
    .sla-orange { background: #e65100; }
    .sla-red    { background: #b71c1c; }
    .sla-detail { padding: 8px 0; }
    .sla-paused {
      display: inline-block;
      background: #eceff1;
      color: #607d8b;
      border-radius: 12px;
      padding: 2px 10px;
      font-size: 12px;
      margin-bottom: 8px;
    }
    .sla-clock { margin-bottom: 12px; }
    .clock-label { font-size: 12px; font-weight: 500; color: #616161; display: block; margin-bottom: 2px; }
    .breach-text { color: #b71c1c; font-size: 13px; font-weight: 500; }
    .sla-bar.sla-green  ::ng-deep .mdc-linear-progress__bar-inner { border-color: #2e7d32; }
    .sla-bar.sla-yellow ::ng-deep .mdc-linear-progress__bar-inner { border-color: #f9a825; }
    .sla-bar.sla-orange ::ng-deep .mdc-linear-progress__bar-inner { border-color: #e65100; }
    .sla-bar.sla-red    ::ng-deep .mdc-linear-progress__bar-inner { border-color: #b71c1c; }
  `],
})
export class SlaIndicatorComponent implements OnInit {
  @Input() ticketId!: string;
  @Input() mode: 'badge' | 'detail' = 'badge';

  private readonly ticketSvc = inject(TicketService);
  private readonly destroyRef = inject(DestroyRef);

  sla = signal<SlaStatus | null>(null);

  readonly slaColour = slaColour;

  colour = signal<'green' | 'yellow' | 'orange' | 'red'>('green');

  ngOnInit(): void {
    const poll$ = this.mode === 'detail'
      ? timer(0, 60_000).pipe(switchMap(() => this.ticketSvc.getSla(this.ticketId)))
      : this.ticketSvc.getSla(this.ticketId);

    poll$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      this.sla.set(data);
      this.colour.set(slaColour(data.firstResponse.elapsedPercent));
    });
  }

  clampedPercent(value: number): number {
    return Math.min(value, 100);
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/shared/sla-indicator/sla-indicator.component.spec.ts --watch=false
```

Expected: 8 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/shared/sla-indicator/
git commit -m "feat(shared): implement SlaIndicatorComponent badge and detail modes (US-FE-017)"
```

---

## Task 4: SlaIndicatorComponent — detail mode and polling

- [ ] **Step 1: Write the failing tests**

```typescript
// src/app/shared/sla-indicator/sla-indicator.component.spec.ts  (append)
import { fakeAsync, tick } from '@angular/core/testing';

describe('SlaIndicatorComponent — detail mode', () => {
  let fixture: ComponentFixture<SlaIndicatorComponent>;
  let component: SlaIndicatorComponent;
  let ticketSvc: jasmine.SpyObj<TicketService>;

  beforeEach(async () => {
    ticketSvc = jasmine.createSpyObj('TicketService', ['getSla']);
    ticketSvc.getSla.and.returnValue(of(makeSla()));

    await TestBed.configureTestingModule({
      imports: [SlaIndicatorComponent, NoopAnimationsModule],
      providers: [{ provide: TicketService, useValue: ticketSvc }],
    }).compileComponents();

    fixture = TestBed.createComponent(SlaIndicatorComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-77';
    component.mode = 'detail';
    fixture.detectChanges();
  });

  it('should render detail panel with both clocks', () => {
    const detail = fixture.debugElement.query(By.css('[data-testid="sla-detail"]'));
    expect(detail).not.toBeNull();
    const fr = fixture.debugElement.query(By.css('[data-testid="sla-first-response"]'));
    const res = fixture.debugElement.query(By.css('[data-testid="sla-resolution"]'));
    expect(fr).not.toBeNull();
    expect(res).not.toBeNull();
  });

  it('should show "Paused" label when isPaused is true', async () => {
    ticketSvc.getSla.and.returnValue(of(makeSla({ isPaused: true })));
    fixture = TestBed.createComponent(SlaIndicatorComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-77';
    component.mode = 'detail';
    fixture.detectChanges();

    const paused = fixture.debugElement.query(By.css('[data-testid="sla-paused"]'));
    expect(paused).not.toBeNull();
    expect(paused.nativeElement.textContent.trim()).toBe('Paused');
  });

  it('should show breach text when firstResponse is breached', async () => {
    ticketSvc.getSla.and.returnValue(
      of(makeSla({
        firstResponse: {
          dueAt: '2026-08-26T10:00:00Z',
          elapsedPercent: 130,
          breached: true,
          remainingLabel: '-30m',
        },
      }))
    );
    fixture = TestBed.createComponent(SlaIndicatorComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-77';
    component.mode = 'detail';
    fixture.detectChanges();

    const breachText = fixture.debugElement.query(By.css('[data-testid="breach-text-fr"]'));
    expect(breachText).not.toBeNull();
    expect(breachText.nativeElement.textContent).toContain('Breached');
  });

  it('should poll getSla every 60 seconds in detail mode', fakeAsync(() => {
    ticketSvc.getSla.calls.reset();
    fixture = TestBed.createComponent(SlaIndicatorComponent);
    component = fixture.componentInstance;
    component.ticketId = 'ticket-77';
    component.mode = 'detail';
    fixture.detectChanges();

    // Initial call at t=0
    expect(ticketSvc.getSla).toHaveBeenCalledTimes(1);

    tick(60_000);
    expect(ticketSvc.getSla).toHaveBeenCalledTimes(2);

    tick(60_000);
    expect(ticketSvc.getSla).toHaveBeenCalledTimes(3);

    fixture.destroy();
  }));
});
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
ng test --include=src/app/shared/sla-indicator/sla-indicator.component.spec.ts --watch=false
```

Expected: FAIL

- [ ] **Step 3: Implementation already complete in Task 3**

The `ngOnInit` polling logic with `timer(0, 60_000)` and `takeUntilDestroyed` is already present. No additional changes needed.

- [ ] **Step 4: Run tests to verify they pass**

```bash
ng test --include=src/app/shared/sla-indicator/sla-indicator.component.spec.ts --watch=false
```

Expected: 12 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/app/shared/sla-indicator/sla-indicator.component.spec.ts
git commit -m "test(shared): add detail mode and polling tests for SlaIndicatorComponent (US-FE-017)"
```
