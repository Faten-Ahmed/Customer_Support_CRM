import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpBackend } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AuthStore } from './auth.store';

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  requiresPasswordChange: boolean;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Admin' | 'Manager' | 'Agent' | 'Customer';
}

export interface PortalRegisterPayload {
  fullName: string;
  fullNameAr: string;
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
  private readonly _currentUser = signal<LoginResponse | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this.authStore.isAuthenticated());

  private readonly rawHttp: HttpClient;

  constructor(private http: HttpClient, handler: HttpBackend) {
    this.rawHttp = new HttpClient(handler);
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>('/api/v1/auth/login', { email, password })
      .pipe(
        tap(res => {
          this.authStore.setToken(res.accessToken);
          this._currentUser.set(res);
        })
      );
  }

  logout(): void {
    this.authStore.clearToken();
    this._currentUser.set(null);
    this.http.post('/api/v1/auth/logout', {}).subscribe();
  }

  clearSession(): void {
    this.authStore.clearToken();
    this._currentUser.set(null);
  }

  refreshToken(): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/v1/auth/refresh', {}).pipe(
      tap(res => {
        this.authStore.setToken(res.accessToken);
        this._currentUser.set(res);
      })
    );
  }

  portalLogin(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>('/api/v1/auth/login', { email, password })
      .pipe(
        tap(res => {
          this.authStore.setToken(res.accessToken);
          this._currentUser.set(res);
        })
      );
  }

  portalRegister(payload: PortalRegisterPayload): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/v1/auth/portal/register', payload);
  }

  portalVerifyEmail(token: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/v1/auth/portal/verify-email', { token });
  }

  resendVerificationEmail(email: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>('/api/v1/auth/portal/resend-verification', { email });
  }

  refresh(): Observable<{ accessToken: string }> {
    return this.http.post<{ accessToken: string }>('/api/v1/auth/refresh', {});
  }

  changePassword(email: string, currentPassword: string, newPassword: string, confirmPassword: string): Observable<MessageResponse> {
    return this.rawHttp.post<MessageResponse>('/api/v1/auth/change-password-first-login', {
      email,
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
      newPassword: password,
      confirmPassword,
    });
  }
}
