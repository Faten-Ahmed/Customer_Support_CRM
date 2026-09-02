import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { AdminShellComponent } from './admin-shell.component';
import { AuthStore } from '../../auth/auth.store';

describe('AdminShellComponent', () => {
  let fixture: ComponentFixture<AdminShellComponent>;
  let component: AdminShellComponent;

  const setupWithRole = async (role: string) => {
    await TestBed.configureTestingModule({
      imports: [AdminShellComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: AuthStore, useValue: { user: () => ({ role }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  };

  it('should create for Admin role', async () => {
    await setupWithRole('Admin');
    expect(component).toBeTruthy();
  });

  it('should show all nav items for Admin', async () => {
    await setupWithRole('Admin');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Users');
    expect(el.textContent).toContain('Departments');
    expect(el.textContent).toContain('Categories');
  });

  it('should hide Users nav item for Manager', async () => {
    await setupWithRole('Manager');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Users');
    expect(el.textContent).toContain('Departments');
  });

  it('should toggle sidenav collapse', async () => {
    await setupWithRole('Admin');
    expect(component.collapsed()).toBe(false);
    component.toggleSidenav();
    expect(component.collapsed()).toBe(true);
  });
});
