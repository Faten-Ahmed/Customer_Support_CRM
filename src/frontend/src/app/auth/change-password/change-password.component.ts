import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../auth.service';

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
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.scss',
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  form = this.fb.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, passwordStrengthValidator()]],
      confirmPassword: ['', Validators.required],
    },
    { validators: confirmMatchValidator }
  );

  submitting = false;

  onSubmit(): void {
    if (this.form.invalid) return;
    this.submitting = true;
    const { currentPassword, newPassword, confirmPassword } = this.form.value as {
      currentPassword: string;
      newPassword: string;
      confirmPassword: string;
    };
    this.authService.changePassword(currentPassword, newPassword, confirmPassword).subscribe({
      next: () => this.router.navigate(['/app']),
      error: (err: { error?: { code?: string } }) => {
        this.submitting = false;
        if (err.error?.code === 'INVALID_CURRENT_PASSWORD') {
          this.form.get('currentPassword')!.setErrors({ invalid: true });
        }
      },
    });
  }
}
