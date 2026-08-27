import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../auth/auth.service';

@Component({
  selector: 'app-portal-login',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './portal-login.component.html',
  styleUrl: './portal-login.component.scss',
})
export class PortalLoginComponent {
  loginForm: FormGroup;
  loading = signal(false);
  errorCode = signal<string | null>(null);
  hidePassword = signal(true);
  unverifiedEmail = signal<string | null>(null);
  resendSent = signal(false);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    this.errorCode.set(null);
    this.loading.set(true);
    const { email, password } = this.loginForm.value;

    this.authService
      .portalLogin(email, password)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/portal/dashboard']),
        error: (err: HttpErrorResponse) => {
          const code = err.error?.code ?? 'SERVER_ERROR';
          this.errorCode.set(code);
          if (code === 'EMAIL_NOT_VERIFIED') {
            this.unverifiedEmail.set(email);
          }
        },
      });
  }

  resendVerification(): void {
    const email = this.unverifiedEmail();
    if (!email) return;
    this.authService.resendVerificationEmail(email).subscribe(() => {
      this.resendSent.set(true);
    });
  }
}
