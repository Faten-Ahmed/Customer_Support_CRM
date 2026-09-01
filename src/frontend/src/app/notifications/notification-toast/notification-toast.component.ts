import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { SignalRService } from '../../core/signalr.service';
import { Notification } from '../notification.service';

export interface ToastItem {
  id: string;
  notification: Notification;
  persistent: boolean;
}

const PERSISTENT_TYPES = new Set(['SlaBreached', 'Critical']);
const AUTO_DISMISS_MS = 3000;
const MAX_TOASTS = 3;

const ENTITY_ROUTES: Record<string, string> = {
  ticket: '/tickets',
  article: '/kb/articles',
  chat: '/chats',
};

@Component({
  selector: 'app-notification-toast',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  styles: [`
    .toast-container {
      position: fixed;
      bottom: 24px;
      right: 24px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 8px;
      pointer-events: none;
    }
    .toast-item {
      pointer-events: all;
      background: #323232;
      color: #fff;
      border-radius: 4px;
      padding: 12px 16px;
      min-width: 280px;
      max-width: 360px;
      box-shadow: 0 3px 10px rgba(0,0,0,0.3);
      display: flex;
      gap: 12px;
      align-items: flex-start;
    }
    .toast-item.persistent { border-left: 4px solid #f44336; }
    .toast-body { flex: 1; min-width: 0; }
    .toast-title { font-weight: 600; font-size: 14px; }
    .toast-text { font-size: 12px; opacity: 0.85; margin-top: 2px; }
    .toast-actions { display: flex; gap: 4px; margin-top: 8px; }
  `],
  template: `
    <div class="toast-container" aria-live="polite">
      @for (toast of toasts(); track toast.id) {
        <div class="toast-item" [class.persistent]="toast.persistent">
          <div class="toast-body">
            <div class="toast-title">{{ toast.notification.title }}</div>
            <div class="toast-text">{{ toast.notification.body }}</div>
            <div class="toast-actions">
              <button mat-button style="color:#90caf9;font-size:12px;padding:0 4px;" (click)="viewEntity(toast)">
                View
              </button>
              <button mat-icon-button style="width:24px;height:24px;line-height:24px;" (click)="dismiss(toast.id)">
                <mat-icon style="font-size:16px;width:16px;height:16px;">close</mat-icon>
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class NotificationToastComponent implements OnInit, OnDestroy {
  private readonly signalR = inject(SignalRService);
  private readonly router = inject(Router);
  private readonly sub = new Subscription();
  private readonly timers = new Map<string, ReturnType<typeof setTimeout>>();

  readonly toasts = signal<ToastItem[]>([]);

  ngOnInit(): void {
    this.sub.add(
      this.signalR.notification$.subscribe(n => this.addToast(n))
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
    this.timers.forEach(t => clearTimeout(t));
  }

  addToast(notification: Notification): void {
    const persistent = PERSISTENT_TYPES.has(notification.type);
    const toast: ToastItem = {
      id: `${notification.id}-${Date.now()}`,
      notification,
      persistent,
    };

    this.toasts.update(list => {
      const next = [...list, toast];
      return next.length > MAX_TOASTS ? next.slice(next.length - MAX_TOASTS) : next;
    });

    if (!persistent) {
      const timer = setTimeout(() => this.dismiss(toast.id), AUTO_DISMISS_MS);
      this.timers.set(toast.id, timer);
    }
  }

  dismiss(id: string): void {
    clearTimeout(this.timers.get(id));
    this.timers.delete(id);
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  viewEntity(toast: ToastItem): void {
    const base = ENTITY_ROUTES[toast.notification.entityType] ?? '/notifications';
    this.router.navigate([base, toast.notification.entityId]);
    this.dismiss(toast.id);
  }
}
