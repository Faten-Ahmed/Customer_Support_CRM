import { Component, Input, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { TicketMessage, TicketService } from '../../ticket.service';
import { SignalRService } from '../../../shared/services/signalr.service';
import * as signalR from '@microsoft/signalr';

@Component({
  selector: 'app-message-thread',
  standalone: true,
  imports: [CommonModule, MatButtonModule],
  templateUrl: './message-thread.component.html',
  styles: [`
    :host { display: block; }

    .thread-container {
      display: flex;
      flex-direction: column;
      gap: 10px;
      padding: 16px;
      min-height: 200px;
      max-height: 520px;
      overflow-y: auto;
      background: #fafafa;
      border: 1px solid #e8e8e8;
      border-radius: 8px;
    }

    .load-more { text-align: center; margin-bottom: 8px; }

    .empty-thread { color: #aaa; text-align: center; margin: auto; }

    /* Internal note */
    .msg-internal {
      background: #fffde7;
      border: 1px solid #f9a825;
      border-left: 4px solid #f9a825;
      border-radius: 6px;
      padding: 10px 14px;
    }
    .msg-internal-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 6px;
    }
    .internal-label {
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      color: #f57f17;
      letter-spacing: 0.4px;
    }

    /* Row layouts */
    .msg-row { display: flex; }
    .msg-row-right { justify-content: flex-end; }
    .msg-row-left { justify-content: flex-start; }

    /* Bubbles */
    .bubble {
      max-width: 68%;
      border-radius: 12px;
      padding: 10px 14px;
    }
    .bubble-agent {
      background: #1976d2;
      color: #fff;
      border-bottom-right-radius: 3px;
    }
    .bubble-agent .msg-meta,
    .bubble-agent .msg-time { color: rgba(255,255,255,0.75); }

    .bubble-customer {
      background: #f0f0f0;
      color: #212121;
      border-bottom-left-radius: 3px;
    }

    .msg-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 5px;
    }
    .msg-meta { font-size: 12px; font-weight: 600; }
    .msg-time { font-size: 11px; margin-left: auto; }
    .msg-body { margin: 0; font-size: 14px; line-height: 1.5; }
  `],
})
export class MessageThreadComponent implements OnInit, OnDestroy {
  @Input() ticketId!: string;

  private readonly ticketService = inject(TicketService);
  private readonly signalRService = inject(SignalRService);

  readonly messages = signal<TicketMessage[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);

  private page = 1;
  private readonly pageSize = 20;
  private connection!: signalR.HubConnection;

  ngOnInit(): void {
    this.loadMessages();
    this.connectSignalR();
  }

  ngOnDestroy(): void {
    this.connection?.stop();
  }

  private loadMessages(): void {
    this.loading.set(true);
    this.ticketService.getMessages(this.ticketId, this.page, this.pageSize).subscribe({
      next: res => {
        this.messages.update(existing => [...res.items, ...existing]);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  loadMore(): void {
    this.page++;
    this.loadMessages();
  }

  get hasMore(): boolean {
    return this.messages().length < this.totalCount();
  }

  appendMessage(msg: TicketMessage): void {
    this.messages.update(list => [...list, msg]);
    this.totalCount.update(t => t + 1);
  }

  isAgentMessage(msg: TicketMessage): boolean {
    return !!msg.authorUserId && !msg.isInternal;
  }

  private connectSignalR(): void {
    this.connection = this.signalRService.getConnection('/hubs/notifications');
    this.connection.start().then(() => {
      this.connection.on('ReceiveMessage', (msg: TicketMessage) => {
        if (msg.ticketId === this.ticketId) {
          this.messages.update(msgs => [...msgs, msg]);
          this.totalCount.update(t => t + 1);
        }
      });
    }).catch(() => { /* SignalR not yet fully wired */ });
  }
}
