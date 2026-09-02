import { Component, signal, OnInit, viewChildren, ElementRef, afterNextRender } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { finalize } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../auth/auth.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatProgressSpinnerModule, MatIconModule, TranslatePipe],
  template: `
    <div class="verify-container">
      <div class="card">
        <div class="brand">{{ 'portal.brand' | translate }}</div>
        <p class="subtitle">{{ 'portal.subtitle' | translate }}</p>

        @if (verified()) {
          <div class="success-state">
            <mat-icon class="success-icon">check_circle</mat-icon>
            <h2>{{ 'verify.verified' | translate }}</h2>
            <p>{{ 'verify.accountActive' | translate }}</p>
            <a mat-raised-button color="primary" routerLink="/portal/login">{{ 'verify.goToSignIn' | translate }}</a>
          </div>
        } @else {
          <h2 class="title">{{ 'verify.title' | translate }}</h2>
          @if (email()) {
            <p class="hint">We sent a 6-digit code to <strong>{{ email() }}</strong></p>
          } @else {
            <p class="hint">{{ 'verify.hint' | translate }}</p>
          }

          <div class="otp-row" (paste)="onPaste($event)">
            @for (i of indices; track i) {
              <input
                #otpInput
                class="otp-box"
                type="text"
                inputmode="numeric"
                maxlength="1"
                autocomplete="one-time-code"
                [attr.data-index]="i"
                (input)="onInput($event, i)"
                (keydown)="onKeydown($event, i)"
              />
            }
          </div>

          @if (error()) {
            <div class="error-banner" role="alert">{{ error() }}</div>
          }

          <button
            mat-raised-button
            color="primary"
            class="submit-btn"
            [disabled]="loading() || otpCode().length < 6"
            (click)="submit()"
          >
            @if (loading()) {
              <mat-spinner diameter="20" />
            } @else {
              {{ 'verify.verify' | translate }}
            }
          </button>

          <div class="resend-row">
            {{ 'verify.noCode' | translate }}
            <button mat-button color="primary" [disabled]="resendLoading()" (click)="resend()">
              {{ 'verify.resend' | translate }}
            </button>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: var(--mat-sys-surface-container-lowest);
    }
    .verify-container {
      padding: 16px;
      width: 100%;
      display: flex;
      justify-content: center;
    }
    .card {
      width: 100%;
      max-width: 400px;
      background: var(--mat-sys-surface);
      border-radius: 16px;
      box-shadow: 0 4px 24px rgba(0,0,0,0.08);
      padding: 2rem;
      text-align: center;
    }
    .brand {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--mat-sys-primary);
      letter-spacing: 0.05em;
    }
    .subtitle {
      font-size: 0.875rem;
      color: var(--mat-sys-on-surface-variant);
      margin: 0.25rem 0 1.5rem;
    }
    .title {
      margin: 0 0 0.5rem;
      font-size: 1.25rem;
    }
    .hint {
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.9rem;
      margin-bottom: 1.5rem;
    }
    .otp-row {
      display: flex;
      gap: 10px;
      justify-content: center;
      margin-bottom: 1.25rem;
    }
    .otp-box {
      width: 44px;
      height: 56px;
      font-size: 1.5rem;
      font-weight: 700;
      text-align: center;
      border: 2px solid var(--mat-sys-outline);
      border-radius: 8px;
      background: var(--mat-sys-surface-container);
      color: var(--mat-sys-on-surface);
      outline: none;
      transition: border-color 0.15s;
      caret-color: transparent;
    }
    .otp-box:focus {
      border-color: var(--mat-sys-primary);
    }
    .error-banner {
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
      border-radius: 6px;
      padding: 10px 14px;
      font-size: 0.875rem;
      margin-bottom: 1rem;
    }
    .submit-btn {
      width: 100%;
      margin-bottom: 1rem;
    }
    .resend-row {
      font-size: 0.875rem;
      color: var(--mat-sys-on-surface-variant);
    }
    .success-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.5rem;
      padding: 1rem 0;
    }
    .success-icon {
      font-size: 3rem;
      width: 3rem;
      height: 3rem;
      color: var(--mat-sys-primary);
    }
  `],
})
export class VerifyEmailComponent implements OnInit {
  readonly indices = [0, 1, 2, 3, 4, 5];
  readonly otpInputs = viewChildren<ElementRef<HTMLInputElement>>('otpInput');

  email = signal('');
  otpCode = signal('');
  loading = signal(false);
  resendLoading = signal(false);
  error = signal<string | null>(null);
  verified = signal(false);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
  ) {
    afterNextRender(() => {
      this.otpInputs()[0]?.nativeElement.focus();
    });
  }

  ngOnInit(): void {
    this.email.set(this.route.snapshot.queryParamMap.get('email') ?? '');
  }

  onInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    const char = input.value.replace(/\D/g, '').slice(-1);
    input.value = char;
    this.syncCode();
    if (char && index < 5) {
      this.otpInputs()[index + 1]?.nativeElement.focus();
    }
    if (this.otpCode().length === 6) {
      this.submit();
    }
  }

  onKeydown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Backspace') {
      const input = this.otpInputs()[index]?.nativeElement;
      if (input?.value === '' && index > 0) {
        this.otpInputs()[index - 1]?.nativeElement.focus();
      }
      setTimeout(() => this.syncCode(), 0);
    }
  }

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const digits = (event.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 6);
    const inputs = this.otpInputs();
    digits.split('').forEach((d, i) => {
      if (inputs[i]) inputs[i].nativeElement.value = d;
    });
    inputs[Math.min(digits.length, 5)]?.nativeElement.focus();
    this.syncCode();
    if (digits.length === 6) this.submit();
  }

  private syncCode(): void {
    const code = this.otpInputs().map(r => r.nativeElement.value).join('');
    this.otpCode.set(code);
  }

  submit(): void {
    if (this.otpCode().length < 6 || this.loading()) return;
    this.error.set(null);
    this.loading.set(true);
    this.authService
      .portalVerifyEmail(this.otpCode())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => this.verified.set(true),
        error: (err: HttpErrorResponse) => {
          const msg = err.error?.errors?.[0]?.message ?? err.error?.message ?? 'Invalid or expired code.';
          this.error.set(msg);
          this.clearBoxes();
        },
      });
  }

  resend(): void {
    if (!this.email()) return;
    this.resendLoading.set(true);
    this.authService
      .resendVerificationEmail(this.email())
      .pipe(finalize(() => this.resendLoading.set(false)))
      .subscribe({ error: () => {} });
  }

  private clearBoxes(): void {
    this.otpInputs().forEach(r => (r.nativeElement.value = ''));
    this.otpCode.set('');
    this.otpInputs()[0]?.nativeElement.focus();
  }
}
