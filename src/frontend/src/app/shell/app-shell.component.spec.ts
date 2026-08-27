import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { vi } from 'vitest';
import { AppShellComponent } from './app-shell.component';
import { AuthStore } from '../auth/auth.store';

describe('AppShellComponent', () => {
  let fixture: ComponentFixture<AppShellComponent>;
  let component: AppShellComponent;

  const mockAuthStore = {
    user: () => ({ sub: 'a1', fullName: 'Omar Hassan', role: 'Agent' }),
    isAuthenticated: () => true,
    clearToken: vi.fn(),
  };

  beforeEach(async () => {
    localStorage.removeItem('sidenav_collapsed');
    vi.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [AppShellComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: AuthStore, useValue: mockAuthStore },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => localStorage.removeItem('sidenav_collapsed'));

  it('should create', () => expect(component).toBeTruthy());

  it('should display logged-in user name', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Omar Hassan');
  });

  it('should display user role badge', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Agent');
  });

  it('should start with sidenav expanded (no localStorage value)', () => {
    expect(component.collapsed()).toBe(false);
  });

  it('should persist collapsed=true to localStorage on toggle', () => {
    component.toggleSidenav();
    expect(component.collapsed()).toBe(true);
    expect(localStorage.getItem('sidenav_collapsed')).toBe('true');
  });

  it('should restore collapsed state from localStorage', () => {
    localStorage.setItem('sidenav_collapsed', 'true');
    const fresh = TestBed.createComponent(AppShellComponent);
    fresh.detectChanges();
    expect((fresh.componentInstance as AppShellComponent).collapsed()).toBe(true);
    fresh.destroy();
  });

  it('should toggle AI assistant panel', () => {
    expect(component.aiOpen()).toBe(false);
    component.toggleAi();
    expect(component.aiOpen()).toBe(true);
  });

  it('should call authStore.clearToken on logout', () => {
    component.logout();
    expect(mockAuthStore.clearToken).toHaveBeenCalled();
  });
});
