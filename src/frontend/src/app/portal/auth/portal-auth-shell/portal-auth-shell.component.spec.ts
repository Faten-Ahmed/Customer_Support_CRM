import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PortalAuthShellComponent } from './portal-auth-shell.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatTabsModule } from '@angular/material/tabs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('PortalAuthShellComponent', () => {
  let component: PortalAuthShellComponent;
  let fixture: ComponentFixture<PortalAuthShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PortalAuthShellComponent,
        NoopAnimationsModule,
        MatTabsModule,
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalAuthShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render a mat-tab-group with two tabs', () => {
    const tabs = fixture.nativeElement.querySelectorAll('.mat-mdc-tab');
    expect(tabs.length).toBe(2);
  });

  it('should apply dir="rtl" when isRtl is true', () => {
    component.isRtl.set(true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.getAttribute('dir')).toBe('rtl');
  });

  it('default isRtl should be false', () => {
    expect(component.isRtl()).toBe(false);
  });
});
