import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Observable } from 'rxjs';
import { AgentTemplateService, TemplateDto } from '../agent-template.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

interface TemplateForm {
  title: string;
  titleAr: string;
  content: string;
  contentAr: string;
}

@Component({
  selector: 'app-template-management',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatExpansionModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatToolbarModule,
    MatChipsModule,
    MatTooltipModule,
    TranslatePipe,
  ],
  template: `
    <div class="template-page">
      <mat-toolbar color="primary">
        <span>{{ 'nav.templates' | translate }}</span>
      </mat-toolbar>

      <div class="content-area">

        <!-- Search + New Template button -->
        <div class="actions-row">
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>{{ 'common.search' | translate }}</mat-label>
            <input matInput [(ngModel)]="searchTerm" (ngModelChange)="onSearch()" />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>

          <button mat-raised-button color="primary" (click)="toggleNewForm()">
            <mat-icon>add</mat-icon>
            {{ 'templates.newTemplate' | translate }}
          </button>
        </div>

        <!-- Inline Create Form -->
        @if (showNewForm()) {
          <mat-card class="form-card">
            <mat-card-header>
              <mat-card-title>{{ editingId() ? ('templates.editTemplate' | translate) : ('templates.newTemplate' | translate) }}</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>{{ 'templates.titleEn' | translate }}</mat-label>
                  <input matInput [(ngModel)]="form.title" name="title" required />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>{{ 'templates.titleAr' | translate }}</mat-label>
                  <input matInput [(ngModel)]="form.titleAr" name="titleAr" dir="rtl" />
                </mat-form-field>

                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>{{ 'templates.contentEn' | translate }}</mat-label>
                  <textarea matInput [(ngModel)]="form.content" name="content" rows="4" required></textarea>
                </mat-form-field>

                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>{{ 'templates.contentAr' | translate }}</mat-label>
                  <textarea matInput [(ngModel)]="form.contentAr" name="contentAr" rows="4" dir="rtl"></textarea>
                </mat-form-field>
              </div>
            </mat-card-content>
            <mat-card-actions align="end">
              <button mat-button (click)="cancelForm()">{{ 'common.cancel' | translate }}</button>
              <button mat-raised-button color="primary"
                      [disabled]="saving() || !form.title || !form.content"
                      (click)="saveTemplate()">
                @if (saving()) {
                  <mat-spinner diameter="18" style="display:inline-block"></mat-spinner>
                } @else {
                  {{ 'common.save' | translate }}
                }
              </button>
            </mat-card-actions>
          </mat-card>
        }

        @if (loading()) {
          <div class="loading-container">
            <mat-spinner diameter="40"></mat-spinner>
          </div>
        } @else {

          <!-- My Templates (Personal) -->
          <mat-card class="section-card">
            <mat-card-header>
              <mat-card-title>{{ 'templates.myTemplates' | translate }}</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (personalTemplates().length === 0) {
                <p class="empty-message">{{ 'templates.noPersonal' | translate }}</p>
              } @else {
                <mat-accordion multi>
                  @for (tpl of personalTemplates(); track tpl.id) {
                    <mat-expansion-panel>
                      <mat-expansion-panel-header>
                        <mat-panel-title>
                          {{ tpl.title }}
                          @if (tpl.category) {
                            <mat-chip class="category-chip" style="margin-left: 8px; font-size: 11px;">{{ tpl.category }}</mat-chip>
                          }
                        </mat-panel-title>
                        <mat-panel-description>
                          <div class="panel-actions" (click)="$event.stopPropagation()">
                            <button mat-icon-button color="primary"
                                    [matTooltip]="'ticket.edit' | translate"
                                    (click)="startEdit(tpl)">
                              <mat-icon>edit</mat-icon>
                            </button>
                            <button mat-icon-button color="warn"
                                    [matTooltip]="'common.delete' | translate"
                                    (click)="deleteTemplate(tpl)">
                              <mat-icon>delete</mat-icon>
                            </button>
                          </div>
                        </mat-panel-description>
                      </mat-expansion-panel-header>
                      <div class="template-preview">
                        <p><strong>EN:</strong> {{ tpl.content }}</p>
                        @if (tpl.contentAr) {
                          <p dir="rtl"><strong>AR:</strong> {{ tpl.contentAr }}</p>
                        }
                      </div>
                    </mat-expansion-panel>
                  }
                </mat-accordion>
              }
            </mat-card-content>
          </mat-card>

          <!-- Global Templates (read-only) -->
          <mat-card class="section-card">
            <mat-card-header>
              <mat-card-title>{{ 'templates.globalTemplates' | translate }}</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (globalTemplates().length === 0) {
                <p class="empty-message">{{ 'templates.noGlobal' | translate }}</p>
              } @else {
                <mat-accordion multi>
                  @for (tpl of globalTemplates(); track tpl.id) {
                    <mat-expansion-panel>
                      <mat-expansion-panel-header>
                        <mat-panel-title>
                          {{ tpl.title }}
                          @if (tpl.category) {
                            <mat-chip class="category-chip" style="margin-left: 8px; font-size: 11px;">{{ tpl.category }}</mat-chip>
                          }
                        </mat-panel-title>
                      </mat-expansion-panel-header>
                      <div class="template-preview">
                        <p><strong>EN:</strong> {{ tpl.content }}</p>
                        @if (tpl.contentAr) {
                          <p dir="rtl"><strong>AR:</strong> {{ tpl.contentAr }}</p>
                        }
                      </div>
                    </mat-expansion-panel>
                  }
                </mat-accordion>
              }
            </mat-card-content>
          </mat-card>

        }
      </div>
    </div>
  `,
  styles: [`
    .template-page {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .content-area {
      padding: 16px;
      flex: 1;
      overflow: auto;
    }

    .actions-row {
      display: flex;
      gap: 12px;
      align-items: center;
      margin-bottom: 16px;
    }

    .search-field {
      flex: 1;
    }

    .form-card {
      margin-bottom: 16px;
    }

    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    .full-width {
      grid-column: 1 / -1;
    }

    .section-card {
      margin-bottom: 16px;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 32px;
    }

    .empty-message {
      text-align: center;
      padding: 16px;
      color: rgba(0,0,0,0.5);
    }

    .template-preview {
      padding: 8px 0;
      white-space: pre-wrap;
    }

    .panel-actions {
      display: flex;
      gap: 4px;
    }

    .category-chip {
      height: 20px;
    }
  `],
})
export class TemplateManagementComponent implements OnInit {
  private readonly templateService = inject(AgentTemplateService);
  private readonly snackBar = inject(MatSnackBar);

  readonly allTemplates = signal<TemplateDto[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly showNewForm = signal(false);
  readonly editingId = signal<string | null>(null);

  searchTerm = '';

  form: TemplateForm = this.emptyForm();

  readonly personalTemplates = () =>
    this.allTemplates().filter(t =>
      t.scope === 'Personal' &&
      (!this.searchTerm || t.title.toLowerCase().includes(this.searchTerm.toLowerCase()))
    );

  readonly globalTemplates = () =>
    this.allTemplates().filter(t =>
      t.scope === 'Global' &&
      (!this.searchTerm || t.title.toLowerCase().includes(this.searchTerm.toLowerCase()))
    );

  ngOnInit(): void {
    this.loadTemplates();
  }

  loadTemplates(): void {
    this.loading.set(true);
    this.templateService.listMyTemplates(undefined, 1, 200).subscribe({
      next: page => {
        this.allTemplates.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  toggleNewForm(): void {
    this.editingId.set(null);
    this.form = this.emptyForm();
    this.showNewForm.update(v => !v);
  }

  startEdit(tpl: TemplateDto): void {
    this.editingId.set(tpl.id);
    this.form = {
      title: tpl.title,
      titleAr: tpl.titleAr,
      content: tpl.content,
      contentAr: tpl.contentAr,
    };
    this.showNewForm.set(true);
  }

  cancelForm(): void {
    this.showNewForm.set(false);
    this.editingId.set(null);
    this.form = this.emptyForm();
  }

  saveTemplate(): void {
    if (!this.form.title || !this.form.content) return;
    this.saving.set(true);

    const payload = {
      title: this.form.title,
      titleAr: this.form.titleAr,
      content: this.form.content,
      contentAr: this.form.contentAr,
    };

    const id = this.editingId();
    const op$: Observable<unknown> = id
      ? this.templateService.updateTemplate(id, payload)
      : this.templateService.createTemplate(payload);

    op$.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelForm();
        this.loadTemplates();
        this.snackBar.open(id ? 'Template updated' : 'Template created', 'OK', { duration: 3000 });
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to save template. Please try again.', 'OK', { duration: 4000 });
      },
    });
  }

  deleteTemplate(tpl: TemplateDto): void {
    if (!window.confirm(`Delete template "${tpl.title}"?`)) return;

    this.templateService.deleteTemplate(tpl.id).subscribe({
      next: () => {
        this.allTemplates.update(list => list.filter(t => t.id !== tpl.id));
        this.snackBar.open('Template deleted', 'OK', { duration: 3000 });
      },
      error: () => {
        this.snackBar.open('Failed to delete template.', 'OK', { duration: 4000 });
      },
    });
  }

  onSearch(): void {
    // Filter is reactive via personalTemplates() / globalTemplates() getters
  }

  private emptyForm(): TemplateForm {
    return { title: '', titleAr: '', content: '', contentAr: '' };
  }
}
