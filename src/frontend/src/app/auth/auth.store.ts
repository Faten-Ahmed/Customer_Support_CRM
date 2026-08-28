import { Injectable, signal, computed } from '@angular/core';

export interface JwtUser {
  sub: string;
  role: 'Admin' | 'Manager' | 'Agent' | 'Customer';
  passwordMustChange: boolean;
  fullName?: string;
  [key: string]: unknown;
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly _token = signal<string | null>(localStorage.getItem('access_token'));
  private readonly _user = signal<JwtUser | null>(this._decodeToken(localStorage.getItem('access_token')));

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly passwordMustChange = computed(() => this._user()?.passwordMustChange ?? false);

  setToken(token: string): void {
    localStorage.setItem('access_token', token);
    this._token.set(token);
    this._user.set(this._decodeToken(token));
  }

  clearToken(): void {
    localStorage.removeItem('access_token');
    this._token.set(null);
    this._user.set(null);
  }

  getToken(): string | null {
    return this._token();
  }

  private _decodeToken(token: string | null): JwtUser | null {
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      // ASP.NET Core emits role as the long-form URI claim key
      const ROLE_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
      const role = payload['role'] ?? payload[ROLE_URI];
      // fullName may come as a single claim or as firstName+lastName
      const fullName = payload['fullName']
        ?? (payload['firstName'] && payload['lastName']
            ? `${payload['firstName']} ${payload['lastName']}`
            : undefined);
      return { ...payload, role, fullName } as JwtUser;
    } catch {
      return null;
    }
  }
}
