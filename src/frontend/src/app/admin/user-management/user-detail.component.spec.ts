import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { UserDetailComponent } from './user-detail.component';
import { UserService, UserDetail } from './user.service';

const mockUser: UserDetail = {
  id: 'u1',
  firstName: 'Omar',
  lastName: 'Ali',
  email: 'omar@test.com',
  role: 'Agent',
  isActive: true,
  availabilityStatus: 'Online',
  createdAt: '2025-01-01T00:00:00Z',
  passwordMustChange: false,
  departments: [{ departmentId: 'd1', departmentName: 'Support', isPrimary: true }],
  skills: [{ categoryId: 'c1', categoryName: 'Networking' }],
};

describe('UserDetailComponent', () => {
  let fixture: ComponentFixture<UserDetailComponent>;
  let component: UserDetailComponent;
  let userService: {
    getById: ReturnType<typeof vi.fn>;
    deactivate: ReturnType<typeof vi.fn>;
    reactivate: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    userService = {
      getById: vi.fn().mockReturnValue(of({ data: mockUser })),
      deactivate: vi.fn().mockReturnValue(of({})),
      reactivate: vi.fn().mockReturnValue(of({})),
    };

    await TestBed.configureTestingModule({
      imports: [UserDetailComponent, NoopAnimationsModule],
      providers: [
        { provide: UserService, useValue: userService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'u1' } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load user on init', () => {
    expect(userService.getById).toHaveBeenCalledWith('u1');
    expect(component.user()).toEqual(mockUser);
  });

  it('should deactivate user', () => {
    component.deactivate();
    expect(userService.deactivate).toHaveBeenCalledWith('u1');
  });
});
