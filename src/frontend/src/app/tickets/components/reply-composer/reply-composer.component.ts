import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, of } from 'rxjs';
import { TicketMessage, TicketService } from '../../ticket.service';
import { Template, TemplateService } from '../../template.service';

@Component({
  selector: 'app-reply-composer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatTooltipModule,
  ],
  templateUrl: './reply-composer.component.html',
  styles: [`
    :host { display: block; }

    .composer {
      border-top: 1px solid #e0e0e0;
      padding: 16px;
      background: #fff;
      transition: background 0.2s;
    }
    .composer-internal { background: #fffde7; }

    /* Radio mode selector */
    .mode-row {
      display: flex;
      gap: 0;
      margin-bottom: 12px;
      border: 1px solid #e0e0e0;
      border-radius: 6px;
      overflow: hidden;
      width: fit-content;
    }
    .mode-option {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 7px 16px;
      cursor: pointer;
      font-size: 13px;
      font-weight: 500;
      color: #555;
      background: #f5f5f5;
      border-right: 1px solid #e0e0e0;
      transition: background 0.15s, color 0.15s;
      user-select: none;
    }
    .mode-option:last-child { border-right: none; }
    .mode-option input[type="radio"] { display: none; }
    .mode-option.active {
      background: #1976d2;
      color: #fff;
    }
    .mode-internal-opt.active {
      background: #f57f17;
      color: #fff;
    }
    .mode-icon { font-size: 14px; }

    /* Internal banner */
    .internal-banner {
      background: #fff8e1;
      border-left: 3px solid #f9a825;
      padding: 7px 12px;
      font-size: 12px;
      color: #6d4c00;
      border-radius: 3px;
      margin-bottom: 10px;
    }

    /* Footer */
    .composer-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-top: 8px;
    }
    .footer-left { display: flex; align-items: center; gap: 12px; }
    .char-count { font-size: 12px; color: #aaa; }
  `],
})
export class ReplyComposerComponent {
  @Input() ticketId!: string;
  @Output() messageSent = new EventEmitter<TicketMessage>();

  private readonly ticketService = inject(TicketService);
  private readonly templateService = inject(TemplateService);
  private readonly dialog = inject(MatDialog);

  readonly replyControl = new FormControl('', Validators.required);
  readonly isInternal = signal(false);
  readonly sending = signal(false);
  readonly templates = signal<Template[]>([]);

  get charCount(): number {
    return (this.replyControl.value ?? '').length;
  }

  setMode(internal: boolean): void {
    this.isInternal.set(internal);
  }

  toggleInternal(): void {
    this.isInternal.update(v => !v);
  }

  send(): void {
    const content = this.replyControl.value ?? '';
    if (!content.trim()) return;
    this.sending.set(true);
    this.ticketService.addMessage(this.ticketId, content, this.isInternal()).subscribe({
      next: msg => {
        this.messageSent.emit(msg);
        this.replyControl.setValue('');
        this.sending.set(false);
      },
      error: () => this.sending.set(false),
    });
  }

  openTemplatePicker(): void {
    this.templateService.list().pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(page => this.templates.set(page.items));
  }

  applyTemplate(template: Template): void {
    this.replyControl.setValue(template.content);
  }
}
