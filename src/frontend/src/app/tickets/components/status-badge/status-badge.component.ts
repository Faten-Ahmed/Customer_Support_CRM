import { Component, Input } from '@angular/core';

const STATUS_CLASSES: Record<string, string> = {
  New: 'badge-grey',
  Assigned: 'badge-blue',
  InProgress: 'badge-green',
  OnHold: 'badge-yellow',
  Escalated: 'badge-red',
  Resolved: 'badge-teal',
  Reopened: 'badge-purple',
  Closed: 'badge-dark',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span [class]="badgeClass">{{ status }}</span>`,
  styles: [`
    span { padding: 2px 8px; border-radius: 4px; font-size: 0.75rem; font-weight: 600; }
    .badge-grey { background: #e5e7eb; color: #374151; }
    .badge-blue { background: #dbeafe; color: #1d4ed8; }
    .badge-green { background: #d1fae5; color: #065f46; }
    .badge-yellow { background: #fef3c7; color: #92400e; }
    .badge-red { background: #fee2e2; color: #991b1b; }
    .badge-teal { background: #ccfbf1; color: #0f766e; }
    .badge-purple { background: #ede9fe; color: #6d28d9; }
    .badge-dark { background: #1f2937; color: #f9fafb; }
  `],
})
export class StatusBadgeComponent {
  @Input() status = '';

  get badgeClass(): string {
    return STATUS_CLASSES[this.status] ?? 'badge-grey';
  }
}
