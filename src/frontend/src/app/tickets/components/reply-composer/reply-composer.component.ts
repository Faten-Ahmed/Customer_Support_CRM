import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TicketMessage, TicketService } from '../../ticket.service';
import { TemplateService } from '../../template.service';

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

  get charCount(): number {
    return (this.replyControl.value ?? '').length;
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
    this.templateService.list().subscribe();
  }
}
