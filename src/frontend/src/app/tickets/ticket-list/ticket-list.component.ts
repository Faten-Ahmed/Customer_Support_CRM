import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  TicketService,
  TicketSummary,
  TicketStatus,
  TicketPriority,
  TicketListParams,
} from '../ticket.service';

@Component({
  selector: 'app-ticket-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatTooltipModule,
  ],
  templateUrl: './ticket-list.component.html',
})
export class TicketListComponent implements OnInit, OnDestroy {
  private readonly ticketService = inject(TicketService);
  protected readonly router = inject(Router);

  readonly tickets = signal<TicketSummary[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);

  readonly statusOptions: TicketStatus[] = [
    'New', 'Assigned', 'InProgress', 'OnHold', 'Escalated', 'Resolved', 'Reopened', 'Closed',
  ];
  readonly priorityOptions: TicketPriority[] = ['Low', 'Medium', 'High', 'Critical'];

  readonly displayedColumns = [
    'ticketNumber', 'subject', 'customer', 'status', 'priority',
    'assignedTo', 'createdAt',
  ];

  pageSize = 20;
  private currentPage = 1;
  private currentParams: TicketListParams = { page: 1, pageSize: 20 };

  searchValue = '';
  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
    ).subscribe(search => {
      this.currentPage = 1;
      this.loadTickets({ ...this.currentParams, search, page: 1 });
    });

    this.loadTickets(this.currentParams);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadTickets(params: TicketListParams): void {
    this.currentParams = params;
    this.loading.set(true);
    this.ticketService.list(params).subscribe({
      next: res => {
        this.tickets.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  statusBadgeClass(status: TicketStatus): string {
    const map: Record<TicketStatus, string> = {
      New: 'badge-blue',
      Assigned: 'badge-indigo',
      InProgress: 'badge-yellow',
      OnHold: 'badge-orange',
      Escalated: 'badge-red',
      Resolved: 'badge-green',
      Reopened: 'badge-purple',
      Closed: 'badge-gray',
    };
    return map[status] ?? '';
  }

  onNewTicket(): void {
    this.router.navigate(['/app/tickets', 'new']);
  }

  onRowClick(id: string): void {
    this.router.navigate(['/app/tickets', id]);
  }

  onSearch(value: string): void {
    this.searchSubject.next(value);
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadTickets({ ...this.currentParams, page: this.currentPage, pageSize: this.pageSize });
  }

  onStatusFilterChange(statuses: TicketStatus[]): void {
    this.currentPage = 1;
    this.loadTickets({ ...this.currentParams, status: statuses, page: 1 });
  }

  onPriorityFilterChange(priority: TicketPriority | ''): void {
    this.currentPage = 1;
    this.loadTickets({ ...this.currentParams, priority: priority || undefined, page: 1 });
  }
}
