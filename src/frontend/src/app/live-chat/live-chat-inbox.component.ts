import {
  Component, OnInit, OnDestroy, inject, signal, ElementRef, ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import { Subscription, firstValueFrom } from 'rxjs';
import { AgentChatHubService, AgentChatMessage, HandoffRequestedEvent } from './agent-chat-hub.service';

interface PendingSession {
  sessionId: string;
  customerName: string;
  receivedAt: Date;
}

interface WaitingSessionDto {
  sessionId: string;
  customerName: string;
  createdAt: string;
}

@Component({
  selector: 'app-live-chat-inbox',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatTooltipModule,
    MatBadgeModule,
    MatDividerModule,
    CdkTextareaAutosize,
  ],
  templateUrl: './live-chat-inbox.component.html',
})
export class LiveChatInboxComponent implements OnInit, OnDestroy {
  @ViewChild('messageList') private messageList!: ElementRef<HTMLDivElement>;

  private readonly hub = inject(AgentChatHubService);
  private readonly http = inject(HttpClient);

  readonly pending = signal<PendingSession[]>([]);
  readonly activeSessionId = signal<string | null>(null);
  readonly activeCustomerName = signal<string | null>(null);
  readonly messages = signal<AgentChatMessage[]>([]);
  readonly customerTyping = signal(false);
  readonly sessionClosed = signal(false);
  readonly accepting = signal(false);

  readonly inputControl = new FormControl('');

  private subs = new Subscription();
  private typingTimer: ReturnType<typeof setTimeout> | null = null;

  async ngOnInit(): Promise<void> {
    // Populate queue with sessions already waiting before this page was opened
    const waiting = await firstValueFrom(
      this.http.get<WaitingSessionDto[]>('/api/v1/chat/sessions/waiting')
    ).catch(() => [] as WaitingSessionDto[]);
    this.pending.set(waiting.map(s => ({
      sessionId: s.sessionId,
      customerName: s.customerName,
      receivedAt: new Date(s.createdAt),
    })));

    this.subs.add(this.hub.handoffRequested$.subscribe((evt: HandoffRequestedEvent) => {
      this.pending.update(list => [
        ...list.filter(s => s.sessionId !== evt.sessionId),
        { sessionId: evt.sessionId, customerName: evt.customerName, receivedAt: new Date() },
      ]);
    }));

    this.subs.add(this.hub.handoffAccepted$.subscribe(evt => {
      // Remove from pending if another agent accepted
      if (evt.sessionId !== this.activeSessionId()) {
        this.pending.update(list => list.filter(s => s.sessionId !== evt.sessionId));
      }
    }));

    this.subs.add(this.hub.message$.subscribe((msg: AgentChatMessage) => {
      if (msg.sessionId === this.activeSessionId()) {
        this.messages.update(m => [...m, msg]);
        this.scrollToBottom();
      }
    }));

    this.subs.add(this.hub.customerTyping$.subscribe(() => {
      this.customerTyping.set(true);
      if (this.typingTimer) clearTimeout(this.typingTimer);
      this.typingTimer = setTimeout(() => this.customerTyping.set(false), 2500);
    }));

    this.subs.add(this.hub.sessionClosed$.subscribe(evt => {
      if (evt.sessionId === this.activeSessionId()) {
        this.sessionClosed.set(true);
      }
    }));

    try {
      await this.hub.connect();
      await this.hub.subscribeToDepartment();
    } catch (err) {
      console.error('Failed to connect or subscribe to department:', err);
    }
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    if (this.typingTimer) clearTimeout(this.typingTimer);
    this.hub.disconnect();
  }

  async acceptSession(session: PendingSession): Promise<void> {
    this.accepting.set(true);
    try {
      await this.hub.acceptHandoff(session.sessionId);
      this.pending.update(list => list.filter(s => s.sessionId !== session.sessionId));
      this.activeSessionId.set(session.sessionId);
      this.activeCustomerName.set(session.customerName);
      this.messages.set([]);
      this.sessionClosed.set(false);
      this.inputControl.setValue('');
    } catch (err) {
      console.error('Failed to accept session:', err);
    } finally {
      this.accepting.set(false);
    }
  }

  async sendMessage(): Promise<void> {
    const body = this.inputControl.value?.trim();
    const sessionId = this.activeSessionId();
    if (!body || !sessionId || this.sessionClosed()) return;
    this.inputControl.setValue('');
    try {
      await this.hub.sendMessage(sessionId, body);
    } catch (err) {
      console.error('Failed to send message:', err);
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      const body = this.inputControl.value?.trim();
      if (body && this.activeSessionId() && !this.sessionClosed()) {
        this.sendMessage();
      }
    }
    const sessionId = this.activeSessionId();
    if (sessionId) {
      this.hub.agentTyping(sessionId).catch(() => {});
    }
  }

  async closeActiveSession(): Promise<void> {
    const sessionId = this.activeSessionId();
    if (!sessionId) return;
    try {
      await this.hub.closeSession(sessionId, 'Resolved');
    } catch (err) {
      console.error('Failed to close session:', err);
    }
    this.sessionClosed.set(true);
  }

  dismissSession(): void {
    this.activeSessionId.set(null);
    this.activeCustomerName.set(null);
    this.messages.set([]);
    this.sessionClosed.set(false);
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.messageList) {
        this.messageList.nativeElement.scrollTop = this.messageList.nativeElement.scrollHeight;
      }
    }, 50);
  }
}
