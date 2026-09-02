import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';

describe('AuthStore', () => {
  let store: AuthStore;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [AuthStore] });
    store = TestBed.inject(AuthStore);
  });

  it('should be unauthenticated initially when localStorage is empty', () => {
    expect(store.isAuthenticated()).toBe(false);
    expect(store.user()).toBeNull();
  });

  it('setToken() should decode JWT and expose user claims', () => {
    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', role: 'Agent', passwordMustChange: false }));
    const fakeJwt = `${header}.${payload}.sig`;

    store.setToken(fakeJwt);
    expect(store.isAuthenticated()).toBe(true);
    expect(store.user()?.role).toBe('Agent');
    expect(store.passwordMustChange()).toBe(false);
  });

  it('clearToken() should reset state', () => {
    store.setToken('a.eyJzdWIiOiIxIn0.s');
    store.clearToken();
    expect(store.isAuthenticated()).toBe(false);
    expect(store.user()).toBeNull();
  });

  it('getToken() should return the stored token', () => {
    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '1', role: 'Agent', passwordMustChange: false }));
    const fakeJwt = `${header}.${payload}.sig`;

    store.setToken(fakeJwt);
    expect(store.getToken()).toBe(fakeJwt);
  });

  it('passwordMustChange() should return true when flag is set', () => {
    const header = btoa(JSON.stringify({ alg: 'HS256' }));
    const payload = btoa(JSON.stringify({ sub: '2', role: 'Agent', passwordMustChange: true }));
    store.setToken(`${header}.${payload}.sig`);
    expect(store.passwordMustChange()).toBe(true);
  });
});
