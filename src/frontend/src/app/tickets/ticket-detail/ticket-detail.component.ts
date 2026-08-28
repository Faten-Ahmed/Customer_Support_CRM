import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { TicketService, TicketDetail, TicketStatus } from '../ticket.service';

export type TabName = 'messages' | 'history' | 'attachments';

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatTooltipModule,
    MatMenuModule,
  ],
  templateUrl: './ticket-detail.component.html',
})
export class TicketDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly ticketService = inject(TicketService);
  protected readonly router = inject(Router);

  readonly ticket = signal<TicketDetail | null>(null);
  readonly loading = signal(true);
  readonly activeTab = signal<TabName>('messages');
  readonly aiPanelOpen = signal(false);

  private readonly destroy$ = new Subject<void>();

  private readonly statusTransitions: Record<TicketStatus, TicketStatus[]> = {
    New: ['Assigned', 'Closed'],
    Assigned: ['InProgress', 'OnHold', 'Closed'],
    InProgress: ['OnHold', 'Resolved', 'Escalated'],
    OnHold: ['InProgress', 'Closed'],
    Escalated: ['InProgress', 'Resolved'],
    Resolved: ['Closed', 'Reopened'],
    Reopened: ['InProgress', 'Closed'],
    Closed: [],
  };

  availableNextStatuses(): TicketStatus[] {
    const current = this.ticket()?.status;
    if (!current) return [];
    return this.statusTransitions[current] ?? [];
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.ticketService.getById(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: t => {
        this.ticket.set(t);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setActiveTab(tab: TabName): void {
    this.activeTab.set(tab);
  }

  toggleAiPanel(): void {
    this.aiPanelOpen.update(v => !v);
  }

  onAssign(): void { /* US-FE-012 */ }
  onTransfer(): void { /* US-FE-012 */ }
  onEscalate(): void { /* US-FE-012 */ }
  onChangeStatus(status: TicketStatus): void { /* US-FE-012 */ }
  onClose(): void { /* US-FE-012 */ }
}
