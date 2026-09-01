import {
  Component, OnInit, OnDestroy, inject, signal, computed, ElementRef, ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subscription } from 'rxjs';
import { ChatHubService, ChatMessage } from '../services/chat-hub.service';

const HANDOFF_TIMEOUT_MS = 3 * 60 * 1000; // 3 minutes

@Component({
  selector: 'app-live-chat',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './live-chat.component.html',
})
export class LiveChatComponent implements OnInit, OnDestroy {
  @ViewChild('messageList') private messageList!: ElementRef<HTMLDivElement>;

  private readonly hub = inject(ChatHubService);
  private readonly router = inject(Router);

  readonly messages = signal<ChatMessage[]>([]);
  readonly sessionId = signal<string | null>(null);
  readonly waitingForAgent = signal(true);
  readonly agentName = signal<string | null>(null);
  readonly agentTyping = signal(false);
  readonly connecting = signal(true);
  readonly closed = signal(false);

  readonly inputControl = new FormControl('');
  readonly canSend = computed(() => !this.waitingForAgent() && !this.closed() && !!this.inputControl.value?.trim());

  private subs = new Subscription();
  private handoffTimer: ReturnType<typeof setTimeout> | null = null;
  private typingTimer: ReturnType<typeof setTimeout> | null = null;

  async ngOnInit(): Promise<void> {
    this.hub.connect();

    this.subs.add(this.hub.message$.subscribe(msg => {
      this.messages.update(m => [...m, msg]);
      this.scrollToBottom();
    }));

    this.subs.add(this.hub.handoffAccepted$.subscribe(evt => {
      this.clearHandoffTimer();
      this.waitingForAgent.set(false);
      this.agentName.set(evt.agentName);
      this.addSystemMessage(`${evt.agentName} joined the chat.`);
    }));

    this.subs.add(this.hub.sessionClosed$.subscribe(() => {
      this.closed.set(true);
      this.addSystemMessage('Chat session ended.');
    }));

    this.subs.add(this.hub.agentTyping$.subscribe(() => {
      this.agentTyping.set(true);
      if (this.typingTimer) clearTimeout(this.typingTimer);
      this.typingTimer = setTimeout(() => this.agentTyping.set(false), 2000);
    }));

    try {
      const id = await this.hub.startSession();
      this.sessionId.set(id);
      this.connecting.set(false);
      this.addSystemMessage('Connected. Waiting for an agent to join...');
      this.startHandoffTimer();
    } catch {
      this.connecting.set(false);
      this.addSystemMessage('Failed to connect. Please try again.');
    }
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.clearHandoffTimer();
    if (this.typingTimer) clearTimeout(this.typingTimer);
    this.hub.disconnect();
  }

  async sendMessage(): Promise<void> {
    const body = this.inputControl.value?.trim();
    if (!body || !this.sessionId() || this.closed()) return;
    this.inputControl.setValue('');
    try {
      await this.hub.sendMessage(this.sessionId()!, body);
    } catch {
      this.addSystemMessage('Failed to send message.');
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      if (this.canSend()) this.sendMessage();
    }
    this.hub.customerTyping(this.sessionId()!).catch(() => {});
  }

  async endChat(): Promise<void> {
    if (this.sessionId()) {
      await this.hub.closeSession(this.sessionId()!).catch(() => {});
    }
    this.closed.set(true);
  }

  goToTickets(): void {
    this.router.navigate(['/portal/tickets']);
  }

  private startHandoffTimer(): void {
    this.handoffTimer = setTimeout(() => {
      if (this.waitingForAgent()) {
        this.addSystemMessage('No agent is available right now. Redirecting you to create a ticket...');
        setTimeout(() => {
          this.hub.disconnect();
          this.router.navigate(['/portal/tickets/new'], { queryParams: { from: 'livechat' } });
        }, 2000);
      }
    }, HANDOFF_TIMEOUT_MS);
  }

  private clearHandoffTimer(): void {
    if (this.handoffTimer) {
      clearTimeout(this.handoffTimer);
      this.handoffTimer = null;
    }
  }

  private addSystemMessage(text: string): void {
    const msg: ChatMessage = {
      id: crypto.randomUUID(),
      sessionId: this.sessionId() ?? '',
      senderRole: 'System',
      body: text,
      sentAt: new Date().toISOString(),
    };
    this.messages.update(m => [...m, msg]);
    this.scrollToBottom();
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.messageList) {
        this.messageList.nativeElement.scrollTop = this.messageList.nativeElement.scrollHeight;
      }
    }, 50);
  }
}
