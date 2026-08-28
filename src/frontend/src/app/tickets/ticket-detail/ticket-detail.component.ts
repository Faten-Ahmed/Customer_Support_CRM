import { Component, inject, signal, OnInit, OnDestroy, ViewChild } from '@angular/core';
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
import { MatTabsModule } from '@angular/material/tabs';
import { MatDialog } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';
import { TicketService, TicketDetail, TicketMessage, TicketStatus } from '../ticket.service';
import { AuthStore } from '../../auth/auth.store';
import { AssignModalComponent } from '../modals/assign-modal/assign-modal.component';
import { EditTicketModalComponent } from '../modals/edit-ticket-modal/edit-ticket-modal.component';
import { EscalateModalComponent } from '../modals/escalate-modal/escalate-modal.component';
import { TransferModalComponent } from '../modals/transfer-modal/transfer-modal.component';
import { StatusChangeModalComponent } from '../modals/status-change-modal/status-change-modal.component';
import { MessageThreadComponent } from '../components/message-thread/message-thread.component';
import { ReplyComposerComponent } from '../components/reply-composer/reply-composer.component';
import { AttachmentPanelComponent } from '../attachment-panel/attachment-panel.component';
import { TicketHistoryComponent } from '../ticket-history/ticket-history.component';

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
    MatTabsModule,
    MatDialogModule,
    MessageThreadComponent,
    ReplyComposerComponent,
    AttachmentPanelComponent,
    TicketHistoryComponent,
  ],
  styles: [`
    :host { display: block; }
    .detail-layout { display: flex; gap: 16px; padding: 16px; }
    .detail-sidebar { width: 220px; flex-shrink: 0; }
    .detail-main { flex: 1; min-width: 0; }
    .metadata-grid { display: grid; grid-template-columns: auto 1fr; gap: 4px 12px; font-size: 13px; }
    .metadata-grid dt { color: #666; white-space: nowrap; }
    .metadata-grid dd { margin: 0; }
    .action-bar { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; padding: 12px 16px; border-bottom: 1px solid #e0e0e0; }
    .ticket-number { font-family: monospace; font-size: 12px; color: #888; }
    .ticket-subject { font-size: 18px; font-weight: 600; flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .action-buttons { display: flex; gap: 8px; flex-wrap: wrap; }
    .sidebar-section { margin-bottom: 16px; }
    .sidebar-title { font-size: 11px; font-weight: 600; text-transform: uppercase; color: #888; margin-bottom: 8px; letter-spacing: 0.5px; }
    .status-badge { display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 12px; font-weight: 500; background: #e3f2fd; color: #1565c0; }
    .tab-content { padding-top: 12px; }
  `],
  templateUrl: './ticket-detail.component.html',
})
export class TicketDetailComponent implements OnInit, OnDestroy {
  @ViewChild(MessageThreadComponent) private thread!: MessageThreadComponent;

  private readonly route = inject(ActivatedRoute);
  private readonly ticketService = inject(TicketService);
  private readonly authStore = inject(AuthStore);
  private readonly dialog = inject(MatDialog);
  protected readonly router = inject(Router);

  readonly ticket = signal<TicketDetail | null>(null);
  readonly loading = signal(true);
  readonly aiPanelOpen = signal(false);
  readonly activeTab = signal<'messages' | 'history'>('messages');

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

  toggleAiPanel(): void {
    this.aiPanelOpen.update(v => !v);
  }

  setActiveTab(tab: 'messages' | 'history'): void {
    this.activeTab.set(tab);
  }

  onMessageSent(msg: TicketMessage): void {
    this.thread?.appendMessage(msg);
  }

  private refreshTicket(ticketId: string): void {
    this.ticketService.getById(ticketId).subscribe(t => this.ticket.set(t));
  }

  onEdit(): void {
    const ticket = this.ticket()!;
    const ref = this.dialog.open(EditTicketModalComponent, {
      width: '520px',
      data: { ticket },
    });
    ref.afterClosed().subscribe((updated: TicketDetail | undefined) => {
      if (updated) this.ticket.set(updated);
    });
  }

  onAssign(): void {
    const ticketId = this.ticket()!.id;
    const jwtUser = this.authStore.user();
    this.ticketService.getAgents().subscribe(agents => {
      const visibleAgents = jwtUser?.role === 'Agent'
        ? agents.filter(a => a.id === jwtUser.sub)
        : agents;
      const ref = this.dialog.open(AssignModalComponent, {
        width: '380px',
        data: { ticketId, agents: visibleAgents },
      });
      ref.afterClosed().subscribe((confirmed: boolean) => {
        if (!confirmed) return;
        this.refreshTicket(ticketId);
      });
    });
  }

  onTransfer(): void {
    const ticketId = this.ticket()!.id;
    this.ticketService.getAgents().subscribe(agents => {
      const ref = this.dialog.open(TransferModalComponent, {
        width: '460px',
        data: { ticketId, agents },
      });
      ref.afterClosed().subscribe((confirmed: boolean) => {
        if (!confirmed) return;
        this.refreshTicket(ticketId);
      });
    });
  }

  onEscalate(): void {
    const ticketId = this.ticket()!.id;
    const ref = this.dialog.open(EscalateModalComponent, {
      width: '420px',
      data: { ticketId },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.refreshTicket(ticketId);
    });
  }

  onChangeStatus(status: TicketStatus): void {
    const ticketId = this.ticket()!.id;
    if (status === 'Resolved') {
      const ref = this.dialog.open(StatusChangeModalComponent, {
        width: '420px',
        data: { ticketId, availableStatuses: ['Resolved'] },
      });
      ref.afterClosed().subscribe((confirmed: boolean) => {
        if (!confirmed) return;
        this.refreshTicket(ticketId);
      });
    } else {
      this.ticketService.changeStatus(ticketId, status, undefined).subscribe({
        next: () => this.refreshTicket(ticketId),
      });
    }
  }

  onClose(): void {
    const ticketId = this.ticket()!.id;
    this.ticketService.changeStatus(ticketId, 'Closed', undefined).subscribe({
      next: () => this.refreshTicket(ticketId),
    });
  }
}
