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

  isAgentMessage(msg: TicketMessage): boolean {
    return !!msg.authorUserId && !msg.isInternal;
  }

  isCustomerMessage(msg: TicketMessage): boolean {
    return !!msg.authorCustomerId && !msg.isInternal;
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
    });
  }
}
