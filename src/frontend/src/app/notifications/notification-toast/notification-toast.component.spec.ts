import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationToastComponent, ToastItem } from './notification-toast.component';
import { SignalRService } from '../../core/signalr.service';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Subject } from 'rxjs';
import { Notification } from '../notification.service';
import { vi } from 'vitest';

const makeNotif = (overrides: Partial<Notification> = {}): Notification => ({
  id: 'n1',
  type: 'TicketAssigned',
  title: 'Test notification',
  body: 'Body text here',
  isRead: false,
  entityType: 'ticket',
  entityId: '10',
  createdAt: new Date().toISOString(),
  ...overrides,
});

describe('NotificationToastComponent', () => {
  let fixture: ComponentFixture<NotificationToastComponent>;
  let component: NotificationToastComponent;
  let notificationSubject: Subject<Notification>;
  const routerMock = { navigate: vi.fn() };

  beforeEach(async () => {
    notificationSubject = new Subject<Notification>();
    const signalRMock = {
      connectAll: vi.fn(),
      notification$: notificationSubject.asObservable(),
    };

    await TestBed.configureTestingModule({
      imports: [NotificationToastComponent, NoopAnimationsModule],
      providers: [
        { provide: SignalRService, useValue: signalRMock },
        { provide: Router, useValue: routerMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationToastComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should add a toast when notification$ emits', () => {
    notificationSubject.next(makeNotif());
    expect(component.toasts().length).toBe(1);
  });

  it('should auto-dismiss after 3000ms for non-persistent type', () => {
    vi.useFakeTimers();
    notificationSubject.next(makeNotif({ type: 'TicketAssigned' }));
    expect(component.toasts().length).toBe(1);
    vi.advanceTimersByTime(3000);
    expect(component.toasts().length).toBe(0);
  });

  it('should NOT auto-dismiss SlaBreached notifications', () => {
    vi.useFakeTimers();
    notificationSubject.next(makeNotif({ type: 'SlaBreached' }));
    vi.advanceTimersByTime(5000);
    expect(component.toasts().length).toBe(1);
  });

  it('should NOT auto-dismiss Critical notifications', () => {
    vi.useFakeTimers();
    notificationSubject.next(makeNotif({ type: 'Critical' }));
    vi.advanceTimersByTime(5000);
    expect(component.toasts().length).toBe(1);
  });

  it('should cap visible toasts at 3, dropping oldest', () => {
    notificationSubject.next(makeNotif({ id: 'a' }));
    notificationSubject.next(makeNotif({ id: 'b' }));
    notificationSubject.next(makeNotif({ id: 'c' }));
    notificationSubject.next(makeNotif({ id: 'd' }));
    expect(component.toasts().length).toBe(3);
    expect(component.toasts().map(t => t.notification.id)).toEqual(['b', 'c', 'd']);
  });

  it('dismiss() should remove the toast by id', () => {
    notificationSubject.next(makeNotif({ id: 'x' }));
    const toastId = component.toasts()[0].id;
    component.dismiss(toastId);
    expect(component.toasts().length).toBe(0);
  });

  it('viewEntity() should navigate and dismiss toast', () => {
    notificationSubject.next(makeNotif({ id: 'y', entityType: 'ticket', entityId: '7' }));
    const toast = component.toasts()[0];
    component.viewEntity(toast);
    expect(routerMock.navigate).toHaveBeenCalledWith(['/tickets', '7']);
    expect(component.toasts().length).toBe(0);
  });

  it('should render toast elements in the DOM', () => {
    notificationSubject.next(makeNotif({ title: 'Hello Toast' }));
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector('.toast-item');
    expect(el).toBeTruthy();
    expect(el.textContent).toContain('Hello Toast');
  });
});
