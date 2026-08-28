import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PortalTicketService } from '../services/portal-ticket.service';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

interface FilePreview {
  file: File;
  previewUrl: string | null;
  isImage: boolean;
}

@Component({
  selector: 'app-portal-submit-ticket',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatCardModule, MatTooltipModule,
  ],
  template: `
    <div class="page-header">
      <a mat-button routerLink="/portal/tickets">
        <mat-icon>arrow_back</mat-icon> Back
      </a>
      <h1>Submit a Support Ticket</h1>
    </div>

    <mat-card>
      <mat-card-content>
        @if (successTicketId()) {
          <div class="success-banner">
            <mat-icon>check_circle</mat-icon>
            <div>
              <strong>Ticket submitted successfully.</strong>
              <p>
                <a [routerLink]="['/portal/tickets', successTicketId()]">View your ticket</a>
              </p>
            </div>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()">
            @if (errorMsg()) {
              <div class="error-banner">{{ errorMsg() }}</div>
            }

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Subject</mat-label>
              <input matInput formControlName="subject" placeholder="Briefly describe the issue" />
              @if (form.get('subject')?.hasError('required') && form.get('subject')?.touched) {
                <mat-error>Subject is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Priority</mat-label>
              <mat-select formControlName="priority">
                <mat-option value="Low">Low</mat-option>
                <mat-option value="Medium">Medium</mat-option>
                <mat-option value="High">High</mat-option>
                <mat-option value="Critical">Critical</mat-option>
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Description</mat-label>
              <textarea matInput formControlName="description" rows="6"
                placeholder="Please describe your issue in detail"></textarea>
              @if (form.get('description')?.hasError('required') && form.get('description')?.touched) {
                <mat-error>Description is required</mat-error>
              }
            </mat-form-field>

            <!-- Attachment picker -->
            <div class="attachment-section">
              <div class="attachment-header">
                <span class="attachment-label">Attachments</span>
                <button type="button" mat-stroked-button (click)="fileInput.click()">
                  <mat-icon>attach_file</mat-icon> Add Files
                </button>
                <input #fileInput type="file" multiple hidden (change)="onFilesSelected(fileInput)" />
              </div>

              <p class="attachment-hint">Max 5 MB per file. Images will be previewed below.</p>

              @if (filePreviews().length > 0) {
                <div class="preview-grid">
                  @for (fp of filePreviews(); track fp.file.name) {
                    <div class="preview-card">
                      @if (fp.isImage && fp.previewUrl) {
                        <img [src]="fp.previewUrl" class="preview-thumb" [alt]="fp.file.name" />
                      } @else {
                        <div class="preview-icon">
                          <mat-icon>insert_drive_file</mat-icon>
                        </div>
                      }
                      <div class="preview-info">
                        <span class="preview-name" [matTooltip]="fp.file.name">{{ fp.file.name }}</span>
                        <span class="preview-size">{{ formatSize(fp.file.size) }}</span>
                      </div>
                      <button type="button" mat-icon-button class="remove-btn"
                              matTooltip="Remove" (click)="removeFile(fp)">
                        <mat-icon>close</mat-icon>
                      </button>
                    </div>
                  }
                </div>
              }
            </div>

            <div class="form-actions">
              <button mat-flat-button color="primary" type="submit" [disabled]="submitting()">
                @if (submitting()) {
                  <mat-spinner diameter="18" />
                  {{ uploadProgress() }}
                } @else {
                  Submit Ticket
                }
              </button>
            </div>
          </form>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .page-header { display: flex; align-items: center; gap: 8px; margin-bottom: 16px; max-width: 680px; margin-left: auto; margin-right: auto; }
    h1 { margin: 0; font-size: 20px; }
    mat-card { max-width: 680px; margin: 0 auto; }
    .full-width { width: 100%; margin-bottom: 8px; }
    .form-actions { text-align: right; margin-top: 16px; }

    .error-banner {
      background: #fdecea; color: #c62828; border-radius: 4px;
      padding: 12px 16px; margin-bottom: 16px; font-size: 14px;
    }
    .success-banner {
      display: flex; align-items: flex-start; gap: 12px;
      background: #e8f5e9; color: #2e7d32; border-radius: 4px;
      padding: 16px; font-size: 14px;
    }
    .success-banner mat-icon { color: #2e7d32; }
    .success-banner p { margin: 4px 0 0; }

    .attachment-section { margin-bottom: 16px; }
    .attachment-header { display: flex; align-items: center; gap: 12px; margin-bottom: 4px; }
    .attachment-label { font-size: 13px; font-weight: 600; color: #555; flex: 1; }
    .attachment-hint { font-size: 12px; color: #999; margin: 0 0 12px; }

    .preview-grid {
      display: flex; flex-wrap: wrap; gap: 10px;
    }
    .preview-card {
      position: relative; width: 120px;
      border: 1px solid #e0e0e0; border-radius: 8px;
      overflow: hidden; background: #fafafa;
      display: flex; flex-direction: column;
    }
    .preview-thumb {
      width: 100%; height: 80px; object-fit: cover; display: block;
    }
    .preview-icon {
      height: 80px; display: flex; align-items: center; justify-content: center;
      background: #f5f5f5;
    }
    .preview-icon mat-icon { font-size: 36px; height: 36px; width: 36px; color: #90a4ae; }
    .preview-info {
      padding: 6px 8px; flex: 1;
    }
    .preview-name {
      display: block; font-size: 11px; font-weight: 500;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .preview-size { font-size: 10px; color: #999; }
    .remove-btn {
      position: absolute; top: 2px; right: 2px;
      width: 24px; height: 24px; line-height: 24px;
      background: rgba(0,0,0,0.4); color: white;
    }
    .remove-btn mat-icon { font-size: 16px; }
  `],
})
export class PortalSubmitTicketComponent {
  private readonly http = inject(HttpClient);
  private readonly ticketService = inject(PortalTicketService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly submitting = signal(false);
  readonly errorMsg = signal<string | null>(null);
  readonly successTicketId = signal<string | null>(null);
  readonly filePreviews = signal<FilePreview[]>([]);
  readonly uploadProgress = signal('');

  form = this.fb.group({
    subject: ['', [Validators.required, Validators.maxLength(500)]],
    priority: ['Medium', Validators.required],
    description: ['', [Validators.required, Validators.maxLength(10000)]],
  });

  onFilesSelected(input: HTMLInputElement): void {
    const files = Array.from(input.files ?? []);
    input.value = '';

    const oversized = files.filter(f => f.size > 5 * 1024 * 1024);
    if (oversized.length) {
      this.errorMsg.set(`File(s) exceed 5 MB limit: ${oversized.map(f => f.name).join(', ')}`);
      return;
    }
    this.errorMsg.set(null);

    const previews: FilePreview[] = files.map(file => {
      const isImage = file.type.startsWith('image/');
      return {
        file,
        isImage,
        previewUrl: isImage ? URL.createObjectURL(file) : null,
      };
    });

    this.filePreviews.update(list => [...list, ...previews]);
  }

  removeFile(fp: FilePreview): void {
    if (fp.previewUrl) URL.revokeObjectURL(fp.previewUrl);
    this.filePreviews.update(list => list.filter(p => p !== fp));
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.errorMsg.set(null);
    this.uploadProgress.set('Creating ticket…');

    const { subject, priority, description } = this.form.value;

    this.http.post<{ id: string; ticketNumber: string }>('/api/v1/portal/tickets', {
      subject, description, priority,
    }).subscribe({
      next: ticket => {
        const files = this.filePreviews();
        if (files.length === 0) {
          this.onComplete(ticket.id);
          return;
        }

        this.uploadProgress.set(`Uploading 0 / ${files.length}…`);
        let done = 0;

        const uploads = files.map(fp =>
          this.ticketService.uploadAttachment(ticket.id, fp.file).pipe(
            catchError(() => of(null))
          )
        );

        forkJoin(uploads).subscribe(() => {
          this.onComplete(ticket.id);
        });
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.uploadProgress.set('');
        if (err.status === 409) {
          this.errorMsg.set('You already have an open ticket. Please wait for it to be resolved.');
        } else if (err.status === 403) {
          this.errorMsg.set('Please verify your email address before submitting a ticket.');
        } else {
          this.errorMsg.set('Something went wrong. Please try again.');
        }
      },
    });
  }

  private onComplete(ticketId: string): void {
    this.filePreviews().forEach(fp => {
      if (fp.previewUrl) URL.revokeObjectURL(fp.previewUrl);
    });
    this.submitting.set(false);
    this.uploadProgress.set('');
    this.successTicketId.set(ticketId);
  }
}
