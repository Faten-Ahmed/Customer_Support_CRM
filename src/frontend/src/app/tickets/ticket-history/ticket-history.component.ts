import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TicketService, TicketHistoryEntry } from '../ticket.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

const FIELD_ICONS: Record<string, string> = {
  Status:            'swap_horiz',
  Priority:          'low_priority',
  AssignedTo:        'person',
  EscalationReason:  'warning',
  Transfer:          'transfer_within_a_station',
  TransferReason:    'notes',
  Subject:           'edit',
  Description:       'description',
  CategoryId:        'category',
};

@Component({
  selector: 'app-ticket-history',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatTooltipModule, TranslatePipe],
  template: `
    <div class="history-panel">
      @if (history().length === 0) {
        <p class="empty-state" data-testid="empty-state">{{ 'ticket.noHistory' | translate }}</p>
      } @else {
        <ul class="history-list">
          @for (entry of history(); track entry.changedAt) {
            <li class="history-entry" data-testid="history-entry">
              <mat-icon
                [class]="'action-icon action-' + entry.fieldChanged"
                data-testid="action-icon"
                [matTooltip]="entry.fieldChanged"
              >{{ iconFor(entry.fieldChanged) }}</mat-icon>

              <div class="entry-body">
                <span class="history-label" data-testid="history-label">
                  {{ labelFor(entry) }}
                </span>
                <span class="entry-meta">
                  {{ entry.changedByName }} &bull;
                  <time [attr.datetime]="entry.changedAt">
                    {{ entry.changedAt | date:'medium' }}
                  </time>
                </span>
              </div>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`
    .history-panel { padding: 8px 0; }
    .history-list { list-style: none; margin: 0; padding: 0; }
    .history-entry {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 10px 0;
      border-bottom: 1px solid #f0f0f0;
    }
    .action-icon { flex-shrink: 0; margin-top: 2px; }
    .action-Status          { color: #1565c0; }
    .action-Priority        { color: #e65100; }
    .action-AssignedTo      { color: #6a1b9a; }
    .action-EscalationReason { color: #b71c1c; }
    .action-Transfer        { color: #00838f; }
    .action-TransferReason  { color: #2e7d32; }
    .action-Subject         { color: #37474f; }
    .action-Description     { color: #4527a0; }
    .action-CategoryId      { color: #880e4f; }
    .entry-body { display: flex; flex-direction: column; }
    .history-label { font-weight: 500; font-size: 14px; }
    .entry-meta { font-size: 12px; color: #757575; margin-top: 2px; }
    .empty-state { color: #9e9e9e; text-align: center; margin-top: 24px; }
  `],
})
export class TicketHistoryComponent implements OnInit {
  @Input() ticketId!: string;

  private readonly ticketSvc = inject(TicketService);

  history = signal<TicketHistoryEntry[]>([]);

  ngOnInit(): void {
    this.ticketSvc.getHistory(this.ticketId).subscribe(page => {
      this.history.set(page.items);
    });
  }

  iconFor(fieldChanged: string): string {
    return FIELD_ICONS[fieldChanged] ?? 'history';
  }

  labelFor(entry: TicketHistoryEntry): string {
    const { fieldChanged, oldValue, newValue } = entry;
    if (oldValue && newValue) return `${fieldChanged}: ${oldValue} → ${newValue}`;
    if (newValue) return `${fieldChanged}: ${newValue}`;
    return fieldChanged;
  }
}
