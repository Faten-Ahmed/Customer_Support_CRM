import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { UserFormDialogComponent } from './user-form-dialog.component';
import { UserService } from './user.service';

describe('UserFormDialogComponent', () => {
  let fixture: ComponentFixture<UserFormDialogComponent>;
  let component: UserFormDialogComponent;
  let userService: { create: ReturnType<typeof vi.fn> };
  let dialogRef: { close: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    userService = { create: vi.fn().mockReturnValue(of({ id: 'u1' })) };
    dialogRef = { close: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [UserFormDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: UserService, useValue: userService },
        { provide: MatDialogRef, useValue: dialogRef },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserFormDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('form should be invalid when empty', () => {
    expect(component.form.invalid).toBe(true);
  });

  it('should call userService.create on valid submit', async () => {
    component.form.setValue({
      firstName: 'Omar',
      lastName: 'Ali',
      email: 'omar@test.com',
      role: 'Agent',
      tempPassword: 'Temp1234!',
      primaryDepartmentId: 'd1',
    });
    component.submit();
    await fixture.whenStable();
    expect(userService.create).toHaveBeenCalled();
    expect(dialogRef.close).toHaveBeenCalledWith({ id: 'u1' });
  });
});
