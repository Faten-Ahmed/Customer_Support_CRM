import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { SlaPolicyService, SlaPolicy, SlaPriority, UpdateSlaPolicyPayload } from './sla-policy.service';

function resolutionAfterResponseValidator(ctrl: AbstractControl): ValidationErrors | null {
  const first = ctrl.get('firstResponseMinutes')?.value as number;
  const res = ctrl.get('resolutionMinutes')?.value as number;
  if (first != null && res != null && res < first) {
    return { resolutionBeforeResponse: true };
  }
  return null;
}

const PRIORITY_ORDER: SlaPriority[] = ['Critical', 'High', 'Medium', 'Low'];

@Component({
  selector: 'app-sla-policy-table',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
  ],
  template: `
    <div class="sla-page">
      <h1 i18n>SLA Policies</h1>

      @for (priority of priorityOrder; track priority) {
        @if (policiesByPriority()[priority]?.length) {
          <h2 class="priority-heading">{{ priority }}</h2>
          <table mat-table [dataSource]="policiesByPriority()[priority]!" class="full-width mat-elevation-z1">

            <ng-container matColumnDef="priority">
              <th mat-header-cell *matHeaderCellDef i18n>Priority</th>
              <td mat-cell *matCellDef="let p">{{ p.priority }}</td>
            </ng-container>

            <ng-container matColumnDef="firstResponse">
              <th mat-header-cell *matHeaderCellDef i18n>First Response (min)</th>
              <td mat-cell *matCellDef="let p">
                @if (editingId() === p.id) {
                  <mat-form-field appearance="outline" class="inline-field">
                    <input matInput type="number" [formControl]="$any(editForm.get('firstResponseMinutes'))" />
                    @if (editForm.get('firstResponseMinutes')?.hasError('min')) {
                      <mat-error i18n>Must be ≥ 1</mat-error>
                    }
                  </mat-form-field>
                } @else {
                  {{ p.firstResponseMinutes }}
                }
              </td>
            </ng-container>

            <ng-container matColumnDef="resolution">
              <th mat-header-cell *matHeaderCellDef i18n>Resolution (min)</th>
              <td mat-cell *matCellDef="let p">
                @if (editingId() === p.id) {
                  <mat-form-field appearance="outline" class="inline-field">
                    <input matInput type="number" [formControl]="$any(editForm.get('resolutionMinutes'))" />
                    @if (editForm.hasError('resolutionBeforeResponse')) {
                      <mat-error i18n>Must be ≥ first response</mat-error>
                    }
                  </mat-form-field>
                } @else {
                  {{ p.resolutionMinutes }}
                }
              </td>
            </ng-container>

            <ng-container matColumnDef="warning">
              <th mat-header-cell *matHeaderCellDef i18n>Warning %</th>
              <td mat-cell *matCellDef="let p">
                @if (editingId() === p.id) {
                  <mat-form-field appearance="outline" class="inline-field">
                    <input matInput type="number" [formControl]="$any(editForm.get('warningThresholdPercent'))" />
                  </mat-form-field>
                } @else {
                  {{ p.warningThresholdPercent }}
                }
              </td>
            </ng-container>

            <ng-container matColumnDef="breach">
              <th mat-header-cell *matHeaderCellDef i18n>Breach %</th>
              <td mat-cell *matCellDef="let p">
                @if (editingId() === p.id) {
                  <mat-form-field appearance="outline" class="inline-field">
                    <input matInput type="number" [formControl]="$any(editForm.get('breachThresholdPercent'))" />
                  </mat-form-field>
                } @else {
                  {{ p.breachThresholdPercent }}
                }
              </td>
            </ng-container>

            <ng-container matColumnDef="critical">
              <th mat-header-cell *matHeaderCellDef i18n>Critical %</th>
              <td mat-cell *matCellDef="let p">
                @if (editingId() === p.id) {
                  <mat-form-field appearance="outline" class="inline-field">
                    <input matInput type="number" [formControl]="$any(editForm.get('criticalBreachThresholdPercent'))" />
                  </mat-form-field>
                } @else {
                  {{ p.criticalBreachThresholdPercent }}
                }
              </td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let p">
                @if (editingId() === p.id) {
                  <button mat-icon-button color="primary" (click)="saveEdit()" [disabled]="editForm.invalid" aria-label="Save">
                    <mat-icon>check</mat-icon>
                  </button>
                  <button mat-icon-button (click)="cancelEdit()" aria-label="Cancel">
                    <mat-icon>close</mat-icon>
                  </button>
                } @else {
                  <button mat-icon-button (click)="startEdit(p)" aria-label="Edit">
                    <mat-icon>edit</mat-icon>
                  </button>
                }
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        }
      }
    </div>
  `,
  styles: [`
    .sla-page { padding: 24px; }
    .priority-heading { margin: 24px 0 8px; font-size: 16px; font-weight: 500; color: #424242; }
    .full-width { width: 100%; margin-bottom: 8px; }
    .inline-field { width: 100px; }
  `],
})
export class SlaPolicyTableComponent implements OnInit {
  private readonly svc = inject(SlaPolicyService);
  private readonly fb = inject(FormBuilder);

  policies = signal<SlaPolicy[]>([]);
  editingId = signal<string | null>(null);
  editForm!: FormGroup;

  policiesByPriority = computed(() =>
    this.policies().reduce((acc, p) => {
      if (!acc[p.priority]) acc[p.priority] = [];
      acc[p.priority].push(p);
      return acc;
    }, {} as Record<string, SlaPolicy[]>)
  );

  readonly priorityOrder: SlaPriority[] = PRIORITY_ORDER;
  readonly displayedColumns = ['priority', 'firstResponse', 'resolution', 'warning', 'breach', 'critical', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.svc.list().subscribe(p => this.policies.set(p));
  }

  startEdit(policy: SlaPolicy): void {
    this.editingId.set(policy.id);
    this.editForm = this.fb.group(
      {
        firstResponseMinutes: [policy.firstResponseMinutes, [Validators.required, Validators.min(1)]],
        resolutionMinutes: [policy.resolutionMinutes, [Validators.required, Validators.min(1)]],
        warningThresholdPercent: [policy.warningThresholdPercent, [Validators.required, Validators.min(1), Validators.max(99)]],
        breachThresholdPercent: [policy.breachThresholdPercent, [Validators.required, Validators.min(1)]],
        criticalBreachThresholdPercent: [policy.criticalBreachThresholdPercent, [Validators.required, Validators.min(1)]],
      },
      { validators: resolutionAfterResponseValidator }
    );
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(): void {
    if (this.editForm.invalid) return;
    const id = this.editingId()!;
    const payload: UpdateSlaPolicyPayload = this.editForm.value;
    this.svc.update(id, payload).subscribe(() => {
      this.editingId.set(null);
      this.load();
    });
  }
}
