import { Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatChipsModule } from '@angular/material/chips';
import { DatePipe } from '@angular/common';
import { TicketService, UnassignedTicketDto } from '../ticket.service';
import { AuthStore } from '../../auth/auth.store';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-unassigned-queue',
  standalone: true,
  imports: [
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatPaginatorModule,
    MatSnackBarModule,
    MatToolbarModule,
    MatChipsModule,
    DatePipe,
    TranslatePipe,
  ],
  template: `
    <div class="unassigned-page">
      <mat-toolbar color="primary">
        <span>{{ 'unassigned.title' | translate }}</span>
        <span class="spacer"></span>
        <button mat-icon-button (click)="loadTickets()">
          <mat-icon>refresh</mat-icon>
        </button>
      </mat-toolbar>

      <div class="content-area">
        <mat-card>
          <mat-card-header>
            <mat-card-title>{{ 'unassigned.subtitle' | translate }}</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            @if (loading()) {
              <div class="loading-container">
                <mat-spinner diameter="40"></mat-spinner>
              </div>
            } @else if (tickets().length === 0) {
              <p class="empty-message">{{ 'unassigned.noTickets' | translate }}</p>
            } @else {
              <table mat-table [dataSource]="tickets()" class="full-width-table">

                <ng-container matColumnDef="ticketNumber">
                  <th mat-header-cell *matHeaderCellDef>{{ 'ticket.ticketNum' | translate }}</th>
                  <td mat-cell *matCellDef="let row">{{ row.ticketNumber }}</td>
                </ng-container>

                <ng-container matColumnDef="subject">
                  <th mat-header-cell *matHeaderCellDef>{{ 'common.subject' | translate }}</th>
                  <td mat-cell *matCellDef="let row">{{ row.subject }}</td>
                </ng-container>

                <ng-container matColumnDef="priority">
                  <th mat-header-cell *matHeaderCellDef>{{ 'common.priority' | translate }}</th>
                  <td mat-cell *matCellDef="let row">
                    <span [class]="'priority-badge priority-' + row.priority.toLowerCase()">
                      {{ row.priority }}
                    </span>
                  </td>
                </ng-container>

                <ng-container matColumnDef="createdAt">
                  <th mat-header-cell *matHeaderCellDef>{{ 'common.created' | translate }}</th>
                  <td mat-cell *matCellDef="let row">{{ row.createdAt | date:'medium' }}</td>
                </ng-container>

                <ng-container matColumnDef="breachTier">
                  <th mat-header-cell *matHeaderCellDef>{{ 'unassigned.breachTier' | translate }}</th>
                  <td mat-cell *matCellDef="let row">
                    <mat-chip [style.background-color]="getBreachColor(row.breachTier)"
                              style="color: white; font-size: 12px;">
                      {{ row.breachTier }}
                    </mat-chip>
                  </td>
                </ng-container>

                <ng-container matColumnDef="actions">
                  <th mat-header-cell *matHeaderCellDef></th>
                  <td mat-cell *matCellDef="let row">
                    <button mat-raised-button color="primary"
                            [disabled]="claiming().has(row.id)"
                            (click)="claimTicket(row)">
                      @if (claiming().has(row.id)) {
                        <mat-spinner diameter="18" style="display:inline-block;"></mat-spinner>
                      } @else {
                        {{ 'unassigned.claim' | translate }}
                      }
                    </button>
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
              </table>

              <mat-paginator
                [length]="totalCount()"
                [pageSize]="pageSize"
                [pageSizeOptions]="[20, 50]"
                (page)="onPageChange($event)">
              </mat-paginator>
            }
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .unassigned-page {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .spacer { flex: 1; }

    .content-area {
      padding: 16px;
      flex: 1;
      overflow: auto;
    }

    .full-width-table {
      width: 100%;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 32px;
    }

    .empty-message {
      text-align: center;
      padding: 32px;
      color: rgba(0,0,0,0.5);
    }

    .priority-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 500;
    }

    .priority-low { background: #e3f2fd; color: #1565c0; }
    .priority-medium { background: #fff3e0; color: #e65100; }
    .priority-high { background: #fce4ec; color: #b71c1c; }
    .priority-critical { background: #f44336; color: white; }
  `],
})
export class UnassignedQueueComponent implements OnInit {
  private readonly ticketService = inject(TicketService);
  private readonly authStore = inject(AuthStore);
  private readonly snackBar = inject(MatSnackBar);

  readonly tickets = signal<UnassignedTicketDto[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly claiming = signal<Set<string>>(new Set());

  readonly pageSize = 20;
  currentPage = 1;

  readonly displayedColumns = ['ticketNumber', 'subject', 'priority', 'createdAt', 'breachTier', 'actions'];

  ngOnInit(): void {
    this.loadTickets();
  }

  loadTickets(): void {
    this.loading.set(true);
    this.ticketService.listUnassigned(this.currentPage, this.pageSize).subscribe({
      next: page => {
        this.tickets.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  claimTicket(ticket: UnassignedTicketDto): void {
    const agentId = this.authStore.user()!.sub;
    const current = new Set(this.claiming());
    current.add(ticket.id);
    this.claiming.set(current);

    this.ticketService.assign(ticket.id, agentId).subscribe({
      next: () => {
        this.tickets.update(list => list.filter(t => t.id !== ticket.id));
        const updated = new Set(this.claiming());
        updated.delete(ticket.id);
        this.claiming.set(updated);
      },
      error: (err) => {
        const updated = new Set(this.claiming());
        updated.delete(ticket.id);
        this.claiming.set(updated);

        if (err?.status === 409) {
          this.snackBar.open('Already claimed — refreshing', 'OK', { duration: 4000 });
          this.loadTickets();
        } else {
          this.snackBar.open('Failed to claim ticket. Please try again.', 'OK', { duration: 4000 });
        }
      },
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.loadTickets();
  }

  getBreachColor(tier: string): string {
    const t = (tier ?? '').toLowerCase();
    if (t === 'critical' || t === 'breached') return '#f44336';
    if (t === 'warning') return '#ff9800';
    return '#4caf50';
  }
}
