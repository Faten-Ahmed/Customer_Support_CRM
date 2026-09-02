import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../auth.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const pw = control.get('password')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return pw && confirm && pw !== confirm ? { passwordMismatch: true } : null;
}

export type PasswordStrength = 'weak' | 'medium' | 'strong';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent implements OnInit {
  resetForm: FormGroup;
  token = signal<string | null>(null);
  loading = signal(false);
  errorCode = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  hidePassword = signal(true);
  hideConfirm = signal(true);
  passwordStrength = signal<PasswordStrength>('weak');

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.resetForm = this.fb.group(
      {
        password: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: passwordMatchValidator }
    );

    this.resetForm.controls['password'].valueChanges.subscribe((val: string) => {
      this.passwordStrength.set(this.getPasswordStrength(val));
    });
  }

  ngOnInit(): void {
    const tok = this.route.snapshot.queryParamMap.get('token');
    this.token.set(tok);
  }

  getPasswordStrength(password: string): PasswordStrength {
    if (!password || password.length < 6) return 'weak';
    const hasUpper = /[A-Z]/.test(password);
    const hasDigit = /\d/.test(password);
    const hasSpecial = /[^A-Za-z0-9]/.test(password);
    const longEnough = password.length >= 8;

    if (longEnough && hasUpper && hasDigit && hasSpecial) return 'strong';
    if (longEnough && (hasUpper || hasDigit)) return 'medium';
    return 'weak';
  }

  onSubmit(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    const tok = this.token();
    if (!tok) {
      this.errorCode.set('MISSING_TOKEN');
      return;
    }

    this.errorCode.set(null);
    this.loading.set(true);
    const { password, confirmPassword } = this.resetForm.value;

    this.authService
      .resetPassword(tok, password, confirmPassword)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => this.successMessage.set('Password reset successfully.'),
        error: (err: HttpErrorResponse) => {
          this.errorCode.set(err.error?.code ?? 'SERVER_ERROR');
        },
      });
  }
}
