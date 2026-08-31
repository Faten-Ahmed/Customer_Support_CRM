import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { BusinessHoursService, BusinessHoursCard } from './business-hours.service';

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const TIMEZONES = ['UTC', 'Asia/Riyadh', 'Asia/Dubai', 'Africa/Cairo', 'America/New_York', 'Europe/London'];

function endAfterStartValidator(ctrl: AbstractControl): ValidationErrors | null {
  const start = ctrl.get('startTime')?.value as string;
  const end = ctrl.get('endTime')?.value as string;
  if (start && end && end <= start) return { endBeforeStart: true };
  return null;
}

@Component({
  selector: 'app-business-hours-editor',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatCheckboxModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
  ],
  template: `
    <div class="bh-page">
      <h1 i18n>Business Hours</h1>
      <div class="cards-grid">
        @for (card of cards(); track card.id) {
          <mat-card [class.unsaved]="isUnsaved(card.id)">
            <mat-card-header>
              <mat-card-title>
                {{ cardLabel(card) }}
                @if (isUnsaved(card.id)) {
                  <span class="unsaved-chip" i18n>Unsaved changes</span>
                }
              </mat-card-title>
            </mat-card-header>

            <mat-card-content [formGroup]="cardForms[card.id]">
              <div class="section-label" i18n>Work Days</div>
              <div class="day-row">
                @for (day of dayNames; track day) {
                  <mat-checkbox
                    [checked]="isDaySelected(card.id, day)"
                    (change)="toggleDay(card.id, day, $event.checked)"
                  >{{ day.slice(0, 3) }}</mat-checkbox>
                }
              </div>

              <div class="time-row">
                <mat-form-field appearance="outline">
                  <mat-label i18n>Start Time</mat-label>
                  <input matInput type="time" formControlName="startTime" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label i18n>End Time</mat-label>
                  <input matInput type="time" formControlName="endTime" />
                  @if (cardForms[card.id].hasError('endBeforeStart')) {
                    <mat-error i18n>End must be after start</mat-error>
                  }
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label i18n>Timezone</mat-label>
                  <mat-select formControlName="timeZone">
                    @for (tz of timezones; track tz) {
                      <mat-option [value]="tz">{{ tz }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              </div>

              <div class="section-label" i18n>Holidays</div>
              <mat-list dense>
                @for (h of card.holidays; track h.id) {
                  <mat-list-item>
                    <span matListItemTitle>{{ h.date }} — {{ h.name }}</span>
                    <button matListItemMeta mat-icon-button color="warn"
                      (click)="deleteHoliday(card.id, h.id)" aria-label="Delete holiday">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </mat-list-item>
                }
              </mat-list>

              <div class="add-holiday-row" [formGroup]="addHolidayForms[card.id]">
                <mat-form-field appearance="outline" style="width:150px">
                  <mat-label i18n>Date</mat-label>
                  <input matInput type="date" formControlName="date" />
                </mat-form-field>
                <mat-form-field appearance="outline" style="flex:1">
                  <mat-label i18n>Name</mat-label>
                  <input matInput formControlName="name" />
                </mat-form-field>
                <button mat-stroked-button
                  (click)="addHoliday(card.id, addHolidayForms[card.id].value.date, addHolidayForms[card.id].value.name)"
                  [disabled]="addHolidayForms[card.id].invalid"
                  i18n>
                  Add
                </button>
              </div>
            </mat-card-content>

            <mat-card-actions align="end">
              <button mat-raised-button color="primary"
                (click)="saveCard(card.id)"
                [disabled]="cardForms[card.id].invalid || !isUnsaved(card.id)"
                i18n>
                Save
              </button>
            </mat-card-actions>
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .bh-page { padding: 24px; }
    .cards-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(480px, 1fr)); gap: 24px; }
    mat-card.unsaved { border: 2px solid #f59e0b; }
    .unsaved-chip { font-size: 11px; background: #fef3c7; color: #92400e; border-radius: 10px; padding: 2px 8px; margin-left: 8px; vertical-align: middle; }
    .section-label { font-size: 12px; font-weight: 500; color: #616161; margin: 12px 0 6px; }
    .day-row { display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 12px; }
    .time-row { display: flex; gap: 12px; flex-wrap: wrap; }
    .add-holiday-row { display: flex; gap: 8px; align-items: center; margin-top: 8px; }
  `],
})
export class BusinessHoursEditorComponent implements OnInit {
  private readonly svc = inject(BusinessHoursService);
  private readonly fb = inject(FormBuilder);

  cards = signal<BusinessHoursCard[]>([]);
  cardForms: Record<string, FormGroup> = {};
  addHolidayForms: Record<string, FormGroup> = {};

  private readonly unsaved = signal<Set<string>>(new Set());

  readonly dayNames = DAY_NAMES;
  readonly timezones = TIMEZONES;

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.svc.list().subscribe(cards => {
      this.cards.set(cards);
      cards.forEach(c => {
        const form = this.fb.group(
          {
            workDays: [c.workDays],
            startTime: [c.startTime, Validators.required],
            endTime: [c.endTime, Validators.required],
            timeZone: [c.timeZone, Validators.required],
          },
          { validators: endAfterStartValidator }
        );
        form.valueChanges.subscribe(() => this.markUnsaved(c.id));
        this.cardForms[c.id] = form;
        this.addHolidayForms[c.id] = this.fb.group({
          date: ['', Validators.required],
          name: ['', Validators.required],
        });
      });
    });
  }

  cardLabel(card: BusinessHoursCard): string {
    return card.departmentId ? `Dept (${card.departmentId.substring(0, 8)}…)` : 'Global';
  }

  isDaySelected(cardId: string, day: string): boolean {
    const days: string[] = this.cardForms[cardId]?.get('workDays')?.value ?? [];
    return days.includes(day);
  }

  toggleDay(cardId: string, day: string, checked: boolean): void {
    const ctrl = this.cardForms[cardId].get('workDays')!;
    const current: string[] = ctrl.value ?? [];
    ctrl.setValue(checked ? [...current, day] : current.filter(d => d !== day));
    this.cardForms[cardId].markAsDirty();
    this.markUnsaved(cardId);
  }

  isUnsaved(cardId: string): boolean {
    return this.unsaved().has(cardId);
  }

  markUnsavedForTest(cardId: string): void {
    this.markUnsaved(cardId);
  }

  private markUnsaved(cardId: string): void {
    this.unsaved.update(s => new Set([...s, cardId]));
  }

  saveCard(cardId: string): void {
    const form = this.cardForms[cardId];
    if (form.invalid) return;
    const v = form.value;
    this.svc.update(cardId, {
      workDays: v.workDays,
      startTime: v.startTime,
      endTime: v.endTime,
      timeZone: v.timeZone,
    }).subscribe(() => {
      form.markAsPristine();
      this.unsaved.update(s => { const n = new Set(s); n.delete(cardId); return n; });
    });
  }

  addHoliday(cardId: string, date: string, name: string): void {
    if (!date || !name) return;
    this.svc.addHoliday(cardId, date, name).subscribe(res => {
      this.cards.update(cards =>
        cards.map(c => c.id === cardId
          ? { ...c, holidays: [...c.holidays, { id: res.id, date, name }] }
          : c
        )
      );
      this.addHolidayForms[cardId].reset();
    });
  }

  deleteHoliday(cardId: string, holidayId: string): void {
    this.svc.deleteHoliday(cardId, holidayId).subscribe(() => {
      this.cards.update(cards =>
        cards.map(c => c.id === cardId
          ? { ...c, holidays: c.holidays.filter(h => h.id !== holidayId) }
          : c
        )
      );
    });
  }
}
