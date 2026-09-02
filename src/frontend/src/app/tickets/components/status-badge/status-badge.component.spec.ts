import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StatusBadgeComponent } from './status-badge.component';

describe('StatusBadgeComponent', () => {
  let fixture: ComponentFixture<StatusBadgeComponent>;
  let component: StatusBadgeComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatusBadgeComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(StatusBadgeComponent);
    component = fixture.componentInstance;
  });

  const cases: { status: string; expectedClass: string }[] = [
    { status: 'New', expectedClass: 'badge-grey' },
    { status: 'Assigned', expectedClass: 'badge-blue' },
    { status: 'InProgress', expectedClass: 'badge-green' },
    { status: 'OnHold', expectedClass: 'badge-yellow' },
    { status: 'Escalated', expectedClass: 'badge-red' },
    { status: 'Resolved', expectedClass: 'badge-teal' },
    { status: 'Reopened', expectedClass: 'badge-purple' },
    { status: 'Closed', expectedClass: 'badge-dark' },
  ];

  cases.forEach(({ status, expectedClass }) => {
    it(`should render ${expectedClass} class for status ${status}`, () => {
      component.status = status;
      fixture.detectChanges();
      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('span')?.classList.contains(expectedClass)).toBe(true);
    });
  });
});
