import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../auth.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

function passwordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl) => {
    const val: string = control.value ?? '';
    const ok = val.length >= 8 && /[A-Z]/.test(val) && /[0-9]/.test(val);
    return ok ? null : { strength: true };
  };
}

function confirmMatchValidator(group: AbstractControl) {
  const pw = group.get('newPassword')?.value;
  const conf = group.get('confirmPassword')?.value;
  return pw === conf ? null : { mismatch: true };
}

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, TranslatePipe],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.scss',
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  form = this.fb.group(
    {
      email: ['', [Validators.required, Validators.email]],
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, passwordStrengthValidator()]],
      confirmPassword: ['', Validators.required],
    },
    { validators: confirmMatchValidator }
  );

  submitting = false;
  serverError = '';

  onSubmit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    this.serverError = '';
    const { email, currentPassword, newPassword, confirmPassword } = this.form.value as {
      email: string;
      currentPassword: string;
      newPassword: string;
      confirmPassword: string;
    };
    this.authService.changePassword(email, currentPassword, newPassword, confirmPassword).subscribe({
      next: () => this.router.navigate(['/app']),
      error: (err: any) => {
        this.submitting = false;
        const message: string = err?.error?.error ?? '';
        if (err?.status === 401 || err?.error?.code === 'INVALID_CURRENT_PASSWORD' || message.toLowerCase().includes('current password')) {
          this.form.get('currentPassword')!.setErrors({ invalid: true });
        } else if (message) {
          this.serverError = message;
        }
      },
    });
  }
}
