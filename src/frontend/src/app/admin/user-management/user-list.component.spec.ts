import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { UserListComponent } from './user-list.component';
import { UserService } from './user.service';

const mockUsers = [
  {
    id: 'u1',
    firstName: 'Omar',
    lastName: 'Ali',
    email: 'omar@test.com',
    role: 'Agent',
    isActive: true,
    availabilityStatus: 'Online',
    createdAt: '2025-01-01T00:00:00Z',
  },
];

describe('UserListComponent', () => {
  let fixture: ComponentFixture<UserListComponent>;
  let component: UserListComponent;
  let userService: {
    list: ReturnType<typeof vi.fn>;
    deactivate: ReturnType<typeof vi.fn>;
    reactivate: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    userService = {
      list: vi.fn().mockReturnValue(of({ items: mockUsers })),
      deactivate: vi.fn().mockReturnValue(of({})),
      reactivate: vi.fn().mockReturnValue(of({})),
    };

    await TestBed.configureTestingModule({
      imports: [UserListComponent, NoopAnimationsModule],
      providers: [
        { provide: UserService, useValue: userService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load users on init', () => {
    expect(userService.list).toHaveBeenCalled();
    expect(component.users().length).toBe(1);
  });

  it('should open dialog when New User is clicked', () => {
    const dialog = fixture.debugElement.injector.get(MatDialog);
    vi.spyOn(dialog, 'open').mockReturnValue({ afterClosed: () => of(null) } as any);
    component.openNewUserDialog();
    expect(dialog.open).toHaveBeenCalled();
  });

  it('should deactivate a user', () => {
    component.deactivate(mockUsers[0] as any);
    expect(userService.deactivate).toHaveBeenCalledWith('u1');
  });
});
