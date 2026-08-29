import {
  Component,
  Input,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TicketService, Attachment } from '../ticket.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

interface UploadProgress {
  filename: string;
  progress: number;
  error?: string;
}

@Component({
  selector: 'app-attachment-panel',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatTooltipModule,
    TranslatePipe,
  ],
  template: `
    <div class="attachment-panel">
      <div
        class="upload-zone"
        [class.drag-over]="isDragging()"
        (dragover)="onDragOver($event)"
        (dragleave)="isDragging.set(false)"
        (drop)="onDrop($event)"
        (click)="fileInput.click()"
        data-testid="upload-zone"
      >
        <mat-icon>cloud_upload</mat-icon>
        <span>{{ 'ticket.uploadHint' | translate }}</span>
        <span class="hint">{{ 'ticket.maxSize' | translate }}</span>
        <input #fileInput type="file" multiple hidden (change)="onFileSelected($event)" />
      </div>

      @for (up of uploads(); track up.filename) {
        <div class="upload-progress" data-testid="upload-progress">
          <span>{{ up.filename }}</span>
          @if (up.error) {
            <span class="upload-error">{{ up.error }}</span>
          } @else {
            <mat-progress-bar mode="determinate" [value]="up.progress" />
          }
        </div>
      }

      <table mat-table [dataSource]="attachments()" class="attachment-table">
        <ng-container matColumnDef="fileName">
          <th mat-header-cell *matHeaderCellDef>{{ 'common.file' | translate }}</th>
          <td mat-cell *matCellDef="let att">
            <a
              [href]="att.presignedUrl"
              target="_blank"
              rel="noopener"
              data-testid="download-link"
            >{{ att.fileName }}</a>
          </td>
        </ng-container>

        <ng-container matColumnDef="size">
          <th mat-header-cell *matHeaderCellDef>{{ 'common.size' | translate }}</th>
          <td mat-cell *matCellDef="let att">{{ formatSize(att.fileSize) }}</td>
        </ng-container>

        <ng-container matColumnDef="uploaderName">
          <th mat-header-cell *matHeaderCellDef>{{ 'ticket.uploadedBy' | translate }}</th>
          <td mat-cell *matCellDef="let att">{{ att.uploaderName ?? '—' }}</td>
        </ng-container>

        <ng-container matColumnDef="uploadedAt">
          <th mat-header-cell *matHeaderCellDef>{{ 'common.date' | translate }}</th>
          <td mat-cell *matCellDef="let att">{{ att.uploadedAt | date:'medium' }}</td>
        </ng-container>

        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef></th>
          <td mat-cell *matCellDef="let att">
            @if (isAgentSig()) {
              <button
                mat-icon-button
                color="warn"
                data-testid="delete-btn"
                (click)="confirmDelete(att)"
                [matTooltip]="'ticket.deleteAttachment' | translate"
              >
                <mat-icon>delete</mat-icon>
              </button>
            }
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;" data-testid="attachment-row"></tr>
      </table>

      @if (attachments().length === 0 && uploads().length === 0) {
        <p class="empty-state" data-testid="empty-state">{{ 'ticket.noAttachments' | translate }}</p>
      }
    </div>
  `,
  styles: [`
    .upload-zone {
      border: 2px dashed #aaa;
      border-radius: 8px;
      padding: 24px;
      text-align: center;
      cursor: pointer;
      margin-bottom: 16px;
      transition: background 0.2s;
    }
    .upload-zone.drag-over { background: #e3f2fd; border-color: #1976d2; }
    .hint { display: block; font-size: 12px; color: #888; }
    .upload-progress { margin: 8px 0; }
    .upload-error { color: #c62828; font-size: 13px; }
    .attachment-table { width: 100%; }
    .empty-state { color: #888; text-align: center; margin-top: 16px; }
  `],
})
export class AttachmentPanelComponent implements OnInit {
  @Input() ticketId!: string;

  private readonly _isAgent = signal(false);
  readonly isAgentSig = this._isAgent.asReadonly();

  @Input() set isAgent(value: boolean) { this._isAgent.set(value); }
  get isAgent(): boolean { return this._isAgent(); }

  private readonly ticketSvc = inject(TicketService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  attachments = signal<Attachment[]>([]);
  uploads = signal<UploadProgress[]>([]);
  isDragging = signal(false);

  displayedColumns = ['fileName', 'size', 'uploaderName', 'uploadedAt', 'actions'];

  ngOnInit(): void {
    this.ticketSvc.getAttachments(this.ticketId).subscribe(list => {
      this.attachments.set(list);
    });
  }

  formatSize(bytes: number): string {
    return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
  }

  onDragOver(e: DragEvent): void {
    e.preventDefault();
    this.isDragging.set(true);
  }

  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.isDragging.set(false);
    const files = Array.from(e.dataTransfer?.files ?? []);
    this.uploadFiles(files);
  }

  onFileSelected(e: Event): void {
    const input = e.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    this.uploadFiles(files);
    input.value = '';
  }

  uploadFiles(files: File[]): void {
    for (const file of files) {
      if (file.size > 5 * 1024 * 1024) {
        this.uploads.update(u => [
          ...u,
          { filename: file.name, progress: 0, error: 'File exceeds 5 MB limit.' },
        ]);
        continue;
      }
      this.uploads.update(u => [...u, { filename: file.name, progress: 0 }]);
      this.ticketSvc.uploadAttachment(this.ticketId, file).subscribe({
        next: att => {
          this.attachments.update(list => [...list, att]);
          this.uploads.update(u => u.filter(up => up.filename !== file.name));
        },
        error: err => {
          const msg =
            err?.error?.code === 'ATTACHMENT_LIMIT_EXCEEDED'
              ? 'Attachment limit exceeded for this ticket.'
              : 'Upload failed. Please try again.';
          this.uploads.update(u =>
            u.map(up => up.filename === file.name ? { ...up, error: msg } : up)
          );
        },
      });
    }
  }

  confirmDelete(att: Attachment): void {
    const ref = this.dialog.open(DeleteConfirmDialogComponent, {
      data: { fileName: att.fileName },
    });
    ref.afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.ticketSvc.deleteAttachment(this.ticketId, att.id).subscribe(() => {
        this.attachments.update(list => list.filter(a => a.id !== att.id));
        this.snack.open(`"${att.fileName}" deleted.`, 'OK', { duration: 3000 });
      });
    });
  }
}

@Component({
  selector: 'app-delete-confirm-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule, TranslatePipe],
  template: `
    <h2 mat-dialog-title>{{ 'ticket.deleteAttachmentTitle' | translate }}</h2>
    <mat-dialog-content>
      Delete <strong>{{ data.fileName }}</strong>? {{ 'ticket.deleteConfirm' | translate }}
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.cancel' | translate }}</button>
      <button mat-button color="warn" [mat-dialog-close]="true">{{ 'common.delete' | translate }}</button>
    </mat-dialog-actions>
  `,
})
export class DeleteConfirmDialogComponent {
  readonly data = inject<{ fileName: string }>(MAT_DIALOG_DATA);
}
