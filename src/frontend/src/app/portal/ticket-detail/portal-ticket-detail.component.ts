import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import {
  PortalTicketService,
  PortalTicketDetail,
  PortalTicketMessage,
  PortalAttachment,
} from '../services/portal-ticket.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-confirm-close-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule, TranslatePipe],
  template: `
    <h2 mat-dialog-title>{{ 'portal.closeTicketTitle' | translate }}</h2>
    <mat-dialog-content>
      <p>{{ 'portal.closeTicketConfirm' | translate }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.cancel' | translate }}</button>
      <button mat-flat-button color="warn" [mat-dialog-close]="true">{{ 'portal.closeTicketTitle' | translate }}</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmCloseDialogComponent {}

@Component({
  selector: 'app-portal-ticket-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, ReactiveFormsModule, RouterLink,
    MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatProgressSpinnerModule, MatCardModule, MatChipsModule, MatTooltipModule,
    MatDialogModule, MatSnackBarModule,
    TranslatePipe,
  ],
  template: `
    <div class="detail-wrap">
      <a mat-button routerLink="/portal/tickets">
        <mat-icon>arrow_back</mat-icon> {{ 'portal.backToTickets' | translate }}
      </a>

      @if (loading()) {
        <div class="center"><mat-spinner diameter="40" /></div>
      } @else if (ticket()) {
        <div class="ticket-header">
          <div>
            <span class="ticket-num">{{ ticket()!.ticketNumber }}</span>
            <h1>{{ ticket()!.subject }}</h1>
            @if (ticket()!.assignedAgentName) {
              <span class="agent-name">{{ 'portal.agentLabel' | translate }}: {{ ticket()!.assignedAgentName }}</span>
            }
          </div>
          <div class="meta">
            <mat-chip [class]="'status-' + ticket()!.status.toLowerCase()">{{ ticket()!.status }}</mat-chip>
            <span class="priority">{{ ticket()!.priority }}</span>
            @if (!ticket()!.closedAt) {
              <button mat-stroked-button color="warn" (click)="closeTicket()">
                <mat-icon>close</mat-icon> {{ 'portal.closeTicketTitle' | translate }}
              </button>
            }
          </div>
        </div>

        @if (ticket()!.description) {
          <mat-card class="desc-card">
            <mat-card-content>
              <p class="desc-label">{{ 'portal.descriptionLabel' | translate }}</p>
              <p>{{ ticket()!.description }}</p>
            </mat-card-content>
          </mat-card>
        }

        <!-- Message thread -->
        <div class="thread" data-testid="message-thread">
          @if (messages().length === 0) {
            <p class="no-messages">{{ 'portal.noMessages' | translate }}</p>
          }
          @for (msg of messages(); track msg.id) {
            <div class="msg-row" [class.outbound]="msg.authorCustomerId != null" data-testid="message-row">
              <div class="bubble" [class.bubble-out]="msg.authorCustomerId != null" [class.bubble-in]="msg.authorUserId != null">
                <span class="msg-author">{{ msg.authorName ?? ('portal.supportFallback' | translate) }}</span>
                <p class="msg-body">{{ msg.body }}</p>
                <span class="msg-time">{{ msg.createdAt | date:'short' }}</span>
              </div>
            </div>
          }
        </div>

        <!-- Reply box or closed banner -->
        @if (ticket()!.closedAt) {
          <div class="closed-banner" data-testid="closed-banner">
            <mat-icon>lock</mat-icon>
            <span>{{ 'portal.ticketClosed' | translate }}</span>
          </div>
        } @else {
          <mat-card class="reply-card">
            <mat-card-content>
              @if (reopenedMessage()) {
                <div class="reopened-banner" data-testid="reopened-banner">
                  <mat-icon>refresh</mat-icon>
                  {{ reopenedMessage() }}
                </div>
              }

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ 'portal.writeReply' | translate }}</mat-label>
                <textarea matInput [formControl]="replyControl" rows="4"
                  [placeholder]="'portal.replyPlaceholder' | translate"></textarea>
                @if (replyControl.hasError('required') && replyControl.touched) {
                  <mat-error>{{ 'portal.replyRequired' | translate }}</mat-error>
                }
              </mat-form-field>

              @if (sendError()) {
                <div class="send-error">{{ sendError() }}</div>
              }

              <div class="reply-actions">
                <button mat-flat-button color="primary"
                        [disabled]="!replyControl.value?.trim() || sending()"
                        (click)="sendReply()">
                  @if (sending()) {
                    <mat-spinner diameter="18" />
                  } @else {
                    {{ 'portal.sendReply' | translate }}
                  }
                </button>
              </div>
            </mat-card-content>
          </mat-card>
        }

        <!-- Attachments -->
        <mat-card class="attachments-card">
          <mat-card-content>
            <div class="attachments-header">
              <span class="section-label">{{ 'portal.attachmentsLabel' | translate }} ({{ attachments().length }})</span>
              @if (!ticket()!.closedAt) {
                <button mat-stroked-button [disabled]="uploading()" (click)="fileInput.click()">
                  @if (uploading()) {
                    <mat-spinner diameter="16" />
                  } @else {
                    <mat-icon>attach_file</mat-icon>
                  }
                  {{ 'portal.attachFile' | translate }}
                </button>
              }
              <input #fileInput type="file" hidden (change)="onFileSelected(fileInput)" />
            </div>

            @if (uploadError()) {
              <div class="send-error">{{ uploadError() }}</div>
            }

            @if (attachments().length === 0) {
              <p class="no-attachments">{{ 'portal.noAttachments' | translate }}</p>
            } @else {
              <div class="attachment-grid">
                @for (a of attachments(); track a.id) {
                  <div class="att-card" data-testid="attachment-row">
                    <!-- Thumbnail or icon -->
                    @if (isImage(a.contentType) && a.presignedUrl) {
                      <a [href]="a.presignedUrl" target="_blank" class="att-thumb-link" [matTooltip]="'portal.previewTooltip' | translate">
                        <img [src]="a.presignedUrl" [alt]="a.fileName" class="att-thumb" />
                      </a>
                    } @else {
                      <div class="att-icon-wrap">
                        <mat-icon>{{ fileIcon(a.contentType) }}</mat-icon>
                      </div>
                    }

                    <!-- Info + actions -->
                    <div class="att-body">
                      <span class="att-name" [matTooltip]="a.fileName">{{ a.fileName }}</span>
                      <span class="att-meta">{{ formatSize(a.fileSize) }} · {{ a.uploaderName ?? ('portal.unknownUploader' | translate) }}</span>
                      <span class="att-meta">{{ a.uploadedAt | date:'shortDate' }}</span>
                    </div>
                    <div class="att-actions">
                      @if (a.presignedUrl) {
                        <a mat-icon-button [href]="a.presignedUrl" [download]="a.fileName"
                           target="_blank" matTooltip="Download">
                          <mat-icon>download</mat-icon>
                        </a>
                        @if (isImage(a.contentType)) {
                          <a mat-icon-button [href]="a.presignedUrl" target="_blank" matTooltip="Open full size">
                            <mat-icon>open_in_new</mat-icon>
                          </a>
                        }
                      }
                    </div>
                  </div>
                }
              </div>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .detail-wrap { max-width: 800px; margin: 0 auto; padding: 0 16px 32px; }
    .ticket-header { display: flex; justify-content: space-between; align-items: flex-start; margin: 16px 0 24px; }
    .ticket-num { font-size: 12px; color: #888; display: block; margin-bottom: 4px; }
    h1 { margin: 0; font-size: 20px; }
    .agent-name { font-size: 12px; color: #666; display: block; margin-top: 4px; }
    .meta { display: flex; flex-direction: column; align-items: flex-end; gap: 8px; }
    .priority { font-size: 12px; color: #666; }
    .desc-card { margin-bottom: 24px; }
    .desc-label { font-size: 12px; color: #888; margin-bottom: 6px; }
    .center { display: flex; justify-content: center; padding: 48px; }

    .thread {
      min-height: 120px; max-height: 480px; overflow-y: auto;
      border: 1px solid #e0e0e0; border-radius: 8px;
      padding: 16px; margin-bottom: 24px;
      display: flex; flex-direction: column; gap: 12px;
    }
    .no-messages { color: #aaa; text-align: center; margin: auto; }
    .msg-row { display: flex; }
    .msg-row.outbound { justify-content: flex-end; }
    .bubble { max-width: 70%; border-radius: 12px; padding: 10px 14px; }
    .bubble-out { background: #e3f2fd; }
    .bubble-in { background: #f5f5f5; }
    .msg-author { font-size: 11px; font-weight: 600; color: #555; display: block; margin-bottom: 4px; }
    .msg-body { margin: 0; font-size: 14px; }
    .msg-time { font-size: 10px; color: #aaa; display: block; margin-top: 6px; text-align: right; }

    .closed-banner {
      display: flex; align-items: center; gap: 10px;
      background: #f5f5f5; border-radius: 8px; padding: 16px;
      color: #666; font-size: 14px;
    }
    .reopened-banner {
      display: flex; align-items: center; gap: 8px;
      background: #e8f5e9; border-radius: 6px; padding: 10px 14px;
      color: #2e7d32; font-size: 13px; margin-bottom: 16px;
    }
    .reply-card { margin-top: 8px; }
    .full-width { width: 100%; }
    .reply-actions { text-align: right; margin-top: 8px; }
    .send-error {
      background: #fdecea; color: #c62828; border-radius: 4px;
      padding: 8px 12px; font-size: 13px; margin-bottom: 8px;
    }
    .status-new { background: #e3f2fd; color: #1565c0; }
    .status-assigned { background: #fff3e0; color: #e65100; }
    .status-inprogress { background: #f3e5f5; color: #6a1b9a; }
    .status-reopened { background: #fce4ec; color: #880e4f; }
    .status-resolved { background: #e8f5e9; color: #2e7d32; }
    .status-closed { background: #f5f5f5; color: #616161; }

    .attachments-card { margin-top: 24px; }
    .attachments-header { display: flex; align-items: center; gap: 12px; margin-bottom: 12px; }
    .section-label { font-size: 13px; font-weight: 600; color: #555; flex: 1; }
    .no-attachments { color: #aaa; font-size: 13px; margin: 0; }

    .attachment-grid { display: flex; flex-wrap: wrap; gap: 10px; }
    .att-card {
      width: 140px; border: 1px solid #e0e0e0; border-radius: 8px;
      overflow: hidden; background: #fafafa;
      display: flex; flex-direction: column;
    }
    .att-thumb-link { display: block; }
    .att-thumb { width: 100%; height: 88px; object-fit: cover; display: block; }
    .att-icon-wrap {
      height: 88px; display: flex; align-items: center; justify-content: center;
      background: #f5f5f5;
    }
    .att-icon-wrap mat-icon { font-size: 36px; height: 36px; width: 36px; color: #90a4ae; }
    .att-body { padding: 6px 8px; flex: 1; min-width: 0; }
    .att-name {
      display: block; font-size: 12px; font-weight: 500;
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .att-meta { display: block; font-size: 10px; color: #888; }
    .att-actions { display: flex; justify-content: flex-end; padding: 0 4px 4px; gap: 2px; }
  `],
})
export class PortalTicketDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ticketService = inject(PortalTicketService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly ticket = signal<PortalTicketDetail | null>(null);
  readonly messages = signal<PortalTicketMessage[]>([]);
  readonly attachments = signal<PortalAttachment[]>([]);
  readonly loading = signal(true);
  readonly sending = signal(false);
  readonly uploading = signal(false);
  readonly sendError = signal<string | null>(null);
  readonly uploadError = signal<string | null>(null);
  readonly reopenedMessage = signal<string | null>(null);

  readonly replyControl = new FormControl('', Validators.required);

  private ticketId = '';

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.ticketId = params['id'];
      this.load();
    });
  }

  private load(): void {
    this.loading.set(true);
    this.ticketService.getById(this.ticketId).subscribe({
      next: t => {
        this.ticket.set(t);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.ticketService.getMessages(this.ticketId).subscribe({
      next: page => this.messages.set(page.items),
    });
    this.ticketService.getAttachments(this.ticketId).subscribe({
      next: list => this.attachments.set(list),
    });
  }

  sendReply(): void {
    const body = this.replyControl.value?.trim();
    if (!body) return;
    const wasResolved = this.ticket()?.status === 'Resolved';
    this.sending.set(true);
    this.sendError.set(null);
    this.reopenedMessage.set(null);
    this.ticketService.addMessage(this.ticketId, body).subscribe({
      next: msg => {
        this.messages.update(list => [...list, msg]);
        this.replyControl.setValue('');
        this.sending.set(false);
        if (wasResolved) {
          this.ticket.update(t => t ? { ...t, status: 'Reopened' } : t);
          this.reopenedMessage.set('Your reply has reopened the ticket.');
        }
      },
      error: () => {
        this.sending.set(false);
        this.sendError.set('Failed to send reply. Please try again.');
      },
    });
  }

  closeTicket(): void {
    const ref = this.dialog.open(ConfirmCloseDialogComponent);
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.ticketService.close(this.ticketId).subscribe({
        next: result => {
          this.ticket.update(t =>
            t ? { ...t, status: 'Closed', closedAt: new Date().toISOString() } : t);
          if (result.surveyUrl) {
            this.snackBar.open(
              'How was your experience?', 'Rate Us',
              { duration: 10000 });
          }
        },
      });
    });
  }

  onFileSelected(input: HTMLInputElement): void {
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    const maxBytes = 5 * 1024 * 1024;
    if (file.size > maxBytes) {
      this.uploadError.set('File exceeds the 5 MB limit.');
      return;
    }

    this.uploadError.set(null);
    this.uploading.set(true);
    this.ticketService.uploadAttachment(this.ticketId, file).subscribe({
      next: a => {
        this.attachments.update(list => [...list, a]);
        this.uploading.set(false);
      },
      error: () => {
        this.uploading.set(false);
        this.uploadError.set('Failed to upload file. Please try again.');
      },
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  isImage(contentType: string): boolean {
    return contentType.startsWith('image/');
  }

  fileIcon(contentType: string): string {
    if (contentType.startsWith('image/')) return 'image';
    if (contentType === 'application/pdf') return 'picture_as_pdf';
    if (contentType.includes('word') || contentType.includes('document')) return 'description';
    if (contentType.includes('sheet') || contentType.includes('excel')) return 'table_chart';
    if (contentType.startsWith('video/')) return 'videocam';
    return 'insert_drive_file';
  }
}
