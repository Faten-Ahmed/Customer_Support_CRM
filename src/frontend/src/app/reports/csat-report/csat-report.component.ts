import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { CsatReport, ReportFilter, ReportService } from '../report.service';

@Component({
  selector: 'app-csat-report',
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
  ],
  templateUrl: './csat-report.component.html',
})
export class CsatReportComponent implements OnInit {
  private readonly reportService = inject(ReportService);
  private readonly fb = inject(FormBuilder);

  readonly report = signal<CsatReport | null>(null);
  readonly loading = signal(false);
  readonly error = signal(false);

  filterForm = this.fb.group({
    dateFrom: [this.defaultFrom()],
    dateTo: [this.defaultTo()],
    departmentId: [''],
  });

  distributionColumns = ['rating', 'count'];
  commentColumns = ['content', 'rating', 'agentName', 'date'];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set(false);
    const filter = this.filterForm.value as ReportFilter;
    this.reportService.getCsatReport(filter).subscribe({
      next: r => { this.report.set(r); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  applyFilter(): void { this.load(); }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(1);
    return d.toISOString().split('T')[0];
  }

  private defaultTo(): string {
    return new Date().toISOString().split('T')[0];
  }
}
