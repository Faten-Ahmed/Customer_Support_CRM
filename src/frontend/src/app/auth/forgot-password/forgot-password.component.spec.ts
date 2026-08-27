import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ForgotPasswordComponent } from './forgot-password.component';
import { AuthService } from '../auth.service';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { provideRouter } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { vi } from 'vitest';

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let authServiceSpy: { forgotPassword: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authServiceSpy = { forgotPassword: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        ForgotPasswordComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
        MatInputModule,
        MatButtonModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should be invalid when email is empty', () => {
    expect(component.forgotForm.valid).toBe(false);
  });

  it('should be invalid with malformed email', () => {
    component.forgotForm.controls['email'].setValue('notanemail');
    expect(component.forgotForm.controls['email'].valid).toBe(false);
  });

  it('should be valid with correct email', () => {
    component.forgotForm.controls['email'].setValue('user@example.com');
    expect(component.forgotForm.valid).toBe(true);
  });

  it('should call forgotPassword on valid submit', async () => {
    authServiceSpy.forgotPassword.mockReturnValue(
      of({ message: 'If that address is registered, an email has been sent.' })
    );

    component.forgotForm.controls['email'].setValue('user@example.com');
    component.onSubmit();
    await fixture.whenStable();

    expect(authServiceSpy.forgotPassword).toHaveBeenCalledWith('user@example.com');
  });

  it('should show success message regardless of whether email exists', async () => {
    authServiceSpy.forgotPassword.mockReturnValue(
      of({ message: 'If that address is registered, an email has been sent.' })
    );

    component.forgotForm.controls['email'].setValue('anyone@example.com');
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.submitted()).toBe(true);
    const successEl = fixture.nativeElement.querySelector('[data-testid="forgot-success"]');
    expect(successEl).toBeTruthy();
  });

  it('should show success even when server returns 404', async () => {
    authServiceSpy.forgotPassword.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404 }))
    );

    component.forgotForm.controls['email'].setValue('nobody@example.com');
    component.onSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.submitted()).toBe(true);
  });

  it('should not submit when form is invalid', () => {
    component.onSubmit();
    expect(authServiceSpy.forgotPassword).not.toHaveBeenCalled();
  });

  it('should show loading state while request is in flight', async () => {
    const subject = new Subject<{ message: string }>();
    authServiceSpy.forgotPassword.mockReturnValue(subject.asObservable());

    component.forgotForm.controls['email'].setValue('user@example.com');
    component.onSubmit();

    expect(component.loading()).toBe(true);

    subject.next({ message: 'ok' });
    subject.complete();
    await fixture.whenStable();

    expect(component.loading()).toBe(false);
  });
});
