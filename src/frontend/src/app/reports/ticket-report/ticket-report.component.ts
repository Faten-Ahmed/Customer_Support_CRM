import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ReportFilter, ReportService, TicketVolumeReport } from '../report.service';

@Component({
  selector: 'app-ticket-report',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatTableModule,
    MatIconModule,
    MatDatepickerModule,
    MatNativeDateModule,
  ],
  templateUrl: './ticket-report.component.html',
})
export class TicketReportComponent implements OnInit {
  private readonly reportService = inject(ReportService);
  private readonly fb = inject(FormBuilder);

  readonly report = signal<TicketVolumeReport | null>(null);
  readonly loading = signal(false);
  readonly error = signal(false);

  filterForm = this.fb.group({
    dateFrom: [this.defaultFrom()],
    dateTo:   [this.defaultTo()],
    groupBy:  ['day'],
  });

  trendColumns = ['date', 'created', 'resolved'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(false);
    const v = this.filterForm.value;
    const filter: ReportFilter = {
      dateFrom: this.toIso(v.dateFrom),
      dateTo:   this.toIso(v.dateTo),
      groupBy:  v.groupBy ?? 'day',
    };
    this.reportService.getTicketReport(filter).subscribe({
      next: r  => { this.report.set(r); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  applyFilter(): void { this.load(); }

  summaryCards(r: TicketVolumeReport) {
    return [
      { label: 'Total Created',    value: r.summary.totalCreated },
      { label: 'Resolved',         value: r.summary.totalResolved },
      { label: 'Closed',           value: r.summary.totalClosed },
      { label: 'Open at End',      value: r.summary.openAtEndOfPeriod },
    ];
  }

  dictToRows(dict: Record<string, number>): { key: string; value: number }[] {
    return Object.entries(dict).map(([key, value]) => ({ key, value }));
  }

  private toIso(d: Date | string | null | undefined): string {
    if (!d) return new Date().toISOString().split('T')[0];
    if (d instanceof Date) return d.toISOString().split('T')[0];
    return d;
  }

  private defaultFrom(): Date {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d;
  }

  private defaultTo(): Date {
    return new Date();
  }
}
