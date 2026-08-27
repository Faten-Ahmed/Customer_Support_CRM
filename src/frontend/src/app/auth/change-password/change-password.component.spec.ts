import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ChangePasswordComponent } from './change-password.component';
import { AuthService } from '../auth.service';
import { vi } from 'vitest';

describe('ChangePasswordComponent', () => {
  let fixture: ComponentFixture<ChangePasswordComponent>;
  let component: ChangePasswordComponent;
  let authServiceSpy: { changePassword: ReturnType<typeof vi.fn> };
  let router: Router;
  let navigateSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    authServiceSpy = { changePassword: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        ChangePasswordComponent,
        ReactiveFormsModule,
        NoopAnimationsModule,
      ],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ChangePasswordComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    navigateSpy = vi.spyOn(router, 'navigate') as unknown as ReturnType<typeof vi.fn>;
    navigateSpy.mockResolvedValue(true);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should require all three fields', () => {
    expect(component.form.valid).toBe(false);
  });

  it('should validate password strength (min 8 chars, uppercase, digit)', () => {
    component.form.get('newPassword')!.setValue('weak');
    expect(component.form.get('newPassword')!.errors?.['strength']).toBeTruthy();
  });

  it('should validate confirm password matches new password', () => {
    component.form.get('newPassword')!.setValue('StrongPass1!');
    component.form.get('confirmPassword')!.setValue('Mismatch1!');
    component.form.get('currentPassword')!.setValue('OldPass1!');
    expect(component.form.errors?.['mismatch']).toBeTruthy();
  });

  it('should call changePassword() on valid submit and navigate to /app', async () => {
    authServiceSpy.changePassword.mockReturnValue(of({ message: 'Password changed successfully.' }));
    component.form.setValue({
      currentPassword: 'OldPass1!',
      newPassword: 'NewPass2@',
      confirmPassword: 'NewPass2@',
    });
    component.onSubmit();
    await fixture.whenStable();

    expect(authServiceSpy.changePassword).toHaveBeenCalledWith('OldPass1!', 'NewPass2@', 'NewPass2@');
    expect(navigateSpy).toHaveBeenCalledWith(['/app']);
  });

  it('should show inline error on 422 INVALID_CURRENT_PASSWORD', async () => {
    authServiceSpy.changePassword.mockReturnValue(
      throwError(() => ({ error: { code: 'INVALID_CURRENT_PASSWORD' } }))
    );
    component.form.setValue({
      currentPassword: 'wrongpass',
      newPassword: 'NewPass2@',
      confirmPassword: 'NewPass2@',
    });
    component.onSubmit();
    await fixture.whenStable();

    expect(component.form.get('currentPassword')!.errors?.['invalid']).toBeTruthy();
  });
});
