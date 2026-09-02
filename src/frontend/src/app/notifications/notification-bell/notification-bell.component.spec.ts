import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationBellComponent } from './notification-bell.component';
import { NotificationService } from '../notification.service';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { vi } from 'vitest';

describe('NotificationBellComponent', () => {
  let fixture: ComponentFixture<NotificationBellComponent>;
  let component: NotificationBellComponent;

  const unreadCountSig = signal(0);
  const notifService = {
    getUnreadCount: vi.fn().mockReturnValue(of({ count: 0 })),
    list: vi.fn().mockReturnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 })),
    markRead: vi.fn().mockReturnValue(of(undefined)),
    markAllRead: vi.fn().mockReturnValue(of(undefined)),
    unreadCount: unreadCountSig.asReadonly(),
    notifications: signal([]).asReadonly(),
    loading: signal(false).asReadonly(),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    notifService.getUnreadCount.mockReturnValue(of({ count: 0 }));

    await TestBed.configureTestingModule({
      imports: [NotificationBellComponent, NoopAnimationsModule],
      providers: [{ provide: NotificationService, useValue: notifService }],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationBellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display bell icon button', () => {
    const btn = fixture.nativeElement.querySelector('button[mat-icon-button]');
    expect(btn).toBeTruthy();
    const icon = btn.querySelector('mat-icon');
    expect(icon.textContent.trim()).toBe('notifications');
  });

  it('should hide badge when unreadCount is 0', () => {
    unreadCountSig.set(0);
    fixture.detectChanges();
    expect(component.showBadge()).toBe(false);
  });

  it('should show badge when unreadCount > 0', () => {
    unreadCountSig.set(5);
    fixture.detectChanges();
    expect(component.showBadge()).toBe(true);
  });

  it('toggleInbox() should flip inboxOpen signal', () => {
    expect(component.inboxOpen()).toBe(false);
    component.toggleInbox();
    expect(component.inboxOpen()).toBe(true);
    component.toggleInbox();
    expect(component.inboxOpen()).toBe(false);
  });

  it('should call getUnreadCount on init', () => {
    expect(notifService.getUnreadCount).toHaveBeenCalled();
  });
});
