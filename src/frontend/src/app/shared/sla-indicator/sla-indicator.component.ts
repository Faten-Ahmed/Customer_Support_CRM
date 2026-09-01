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
import { timer, of } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
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
          <div class="sla-paused" data-testid="sla-paused" i18n>Paused</div>
        }

        <div class="sla-clock" data-testid="sla-first-response">
          <span class="clock-label" i18n>First Response</span>
          @if (sla()!.firstResponse.breached) {
            <span class="breach-text" data-testid="breach-text-fr">
              ⚠ <ng-container i18n>Breached</ng-container> {{ sla()!.firstResponse.remainingLabel | slice:1 }} <ng-container i18n>ago</ng-container>
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

        <div class="sla-clock" data-testid="sla-resolution">
          <span class="clock-label" i18n>Resolution</span>
          @if (sla()!.resolution.breached) {
            <span class="breach-text" data-testid="breach-text-res">
              ⚠ <ng-container i18n>Breached</ng-container> {{ sla()!.resolution.remainingLabel | slice:1 }} <ng-container i18n>ago</ng-container>
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
  colour = signal<'green' | 'yellow' | 'orange' | 'red'>('green');

  readonly slaColour = slaColour;

  ngOnInit(): void {
    const fetch$ = this.ticketSvc.getSla(this.ticketId).pipe(
      catchError(() => of(null))
    );

    const poll$ = this.mode === 'detail'
      ? timer(0, 60_000).pipe(switchMap(() => fetch$))
      : fetch$;

    poll$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      if (data) {
        this.sla.set(data);
        this.colour.set(slaColour(data.firstResponse.elapsedPercent));
      }
    });
  }

  clampedPercent(value: number): number {
    return Math.min(value, 100);
  }
}
