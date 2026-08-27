import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AuthStore } from './auth.store';

export interface AuthUser {
  id: string;
  email: string;
  role: 'Admin' | 'Manager' | 'Agent' | 'PortalUser';
  passwordMustChange: boolean;
}

export interface LoginResponse {
  accessToken: string;
  user: AuthUser;
}

export interface PortalRegisterPayload {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface MessageResponse {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authStore = inject(AuthStore);
  private readonly _currentUser = signal<AuthUser | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this.authStore.isAuthenticated());

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>('/api/v1/auth/login', { email, password })
      .pipe(
        tap(res => {
          this.authStore.setToken(res.accessToken);
          this._currentUser.set(res.user);
        })
      );
  }

  logout(): void {
    this.authStore.clearToken();
    this._currentUser.set(null);
    this.http.post('/api/v1/auth/logout', {}).subscribe();
  }

  refreshToken(): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/v1/auth/refresh', {}).pipe(
      tap(res => {
        this.authStore.setToken(res.accessToken);
        this._currentUser.set(res.user);
      })
    );
  }

  portalLogin(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>('/api/v1/auth/login', { email, password })
      .pipe(
        tap(res => {
          this.authStore.setToken(res.accessToken);
          this._currentUser.set(res.user);
        })
      );
  }

  portalRegister(payload: PortalRegisterPayload): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/v1/auth/portal/register', payload);
  }

  resendVerificationEmail(email: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/v1/auth/portal/resend-verification', { email });
  }

  refresh(): Observable<{ accessToken: string }> {
    return this.http.post<{ accessToken: string }>('/api/v1/auth/refresh', {});
  }

  changePassword(currentPassword: string, newPassword: string, confirmPassword: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/v1/auth/change-password-first-login', {
      currentPassword,
      newPassword,
      confirmPassword,
    });
  }

  forgotPassword(email: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/v1/auth/forgot-password', { email });
  }

  resetPassword(token: string, password: string, confirmPassword: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/v1/auth/reset-password', {
      token,
      password,
      confirmPassword,
    });
  }
}
