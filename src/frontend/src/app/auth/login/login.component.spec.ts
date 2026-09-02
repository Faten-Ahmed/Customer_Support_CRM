import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../auth.service';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { of, throwError, Subject } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: { login: ReturnType<typeof vi.fn>; isAuthenticated: ReturnType<typeof vi.fn> };
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    authServiceSpy = {
      login: vi.fn(),
      isAuthenticated: vi.fn().mockReturnValue(false),
    };

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
        MatProgressSpinnerModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigate') as unknown as ReturnType<typeof vi.fn>;
    navigateSpy.mockResolvedValue(true);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have an invalid form when empty', () => {
    expect(component.loginForm.valid).toBe(false);
  });

  it('should mark email invalid with bad format', () => {
    component.loginForm.controls['email'].setValue('notanemail');
    expect(component.loginForm.controls['email'].valid).toBe(false);
  });

  it('should mark email valid with correct format', () => {
    component.loginForm.controls['email'].setValue('user@example.com');
    expect(component.loginForm.controls['email'].valid).toBe(true);
  });

  it('should mark password invalid when empty', () => {
    component.loginForm.controls['password'].setValue('');
    expect(component.loginForm.controls['password'].valid).toBe(false);
  });

  it('should call AuthService.login on valid submit', async () => {
    authServiceSpy.login.mockReturnValue(
      of({ accessToken: 'tok', user: { id: '1', email: 'a@b.com', role: 'Agent', passwordMustChange: false } })
    );

    component.loginForm.setValue({ email: 'staff@azmsquad.com', password: 'Password1!' });
    component.onSubmit();
    await fixture.whenStable();

    expect(authServiceSpy.login).toHaveBeenCalledWith('staff@azmsquad.com', 'Password1!');
    expect(navigateSpy).toHaveBeenCalledWith(['/app']);
  });

  it('should set loading to true while request is in flight', async () => {
    const subject = new Subject<any>();
    authServiceSpy.login.mockReturnValue(subject.asObservable());

    component.loginForm.setValue({ email: 'staff@azmsquad.com', password: 'Password1!' });
    component.onSubmit();

    expect(component.loading()).toBe(true);

    subject.next({ accessToken: 'tok', user: { id: '1', email: 'a@b.com', role: 'Agent', passwordMustChange: false } });
    subject.complete();
    await fixture.whenStable();

    expect(component.loading()).toBe(false);
  });

  it('should show ACCOUNT_INACTIVE banner on 401 with that code', async () => {
    authServiceSpy.login.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 401, error: { code: 'ACCOUNT_INACTIVE' } }))
    );

    component.loginForm.setValue({ email: 'staff@azmsquad.com', password: 'Password1!' });
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.errorCode()).toBe('ACCOUNT_INACTIVE');
    const banner = fixture.nativeElement.querySelector('[data-testid="error-banner"]');
    expect(banner?.textContent).toContain('deactivated');
  });

  it('should redirect to /change-password on 423', async () => {
    authServiceSpy.login.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 423, error: { code: 'PASSWORD_CHANGE_REQUIRED' } }))
    );

    component.loginForm.setValue({ email: 'staff@azmsquad.com', password: 'Password1!' });
    component.onSubmit();
    await fixture.whenStable();

    expect(navigateSpy).toHaveBeenCalledWith(['/change-password']);
  });

  it('should not call login when form is invalid', () => {
    component.loginForm.setValue({ email: '', password: '' });
    component.onSubmit();
    expect(authServiceSpy.login).not.toHaveBeenCalled();
  });

  it('should redirect to /app if already authenticated', () => {
    authServiceSpy.isAuthenticated.mockReturnValue(true);
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(navigateSpy).toHaveBeenCalledWith(['/app']);
  });
});
