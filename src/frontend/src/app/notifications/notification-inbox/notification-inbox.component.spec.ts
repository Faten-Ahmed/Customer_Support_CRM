import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationInboxComponent } from './notification-inbox.component';
import { NotificationService, Notification } from '../notification.service';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { vi } from 'vitest';

const makeNotif = (overrides: Partial<Notification> = {}): Notification => ({
  id: 'n1',
  type: 'TicketAssigned',
  title: 'Ticket assigned',
  body: 'You have a new ticket',
  isRead: false,
  entityType: 'ticket',
  entityId: '42',
  createdAt: new Date().toISOString(),
  ...overrides,
});

describe('NotificationInboxComponent', () => {
  let fixture: ComponentFixture<NotificationInboxComponent>;
  let component: NotificationInboxComponent;

  const notifsSig = signal<Notification[]>([]);
  const loadingSig = signal(false);
  const unreadSig = signal(0);

  const notifService = {
    list: vi.fn().mockReturnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 })),
    markRead: vi.fn().mockReturnValue(of(undefined)),
    markAllRead: vi.fn().mockReturnValue(of(undefined)),
    notifications: notifsSig.asReadonly(),
    loading: loadingSig.asReadonly(),
    unreadCount: unreadSig.asReadonly(),
  };

  const routerMock = { navigate: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    notifService.list.mockReturnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 20 }));
    notifService.markRead.mockReturnValue(of(undefined));
    notifService.markAllRead.mockReturnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [NotificationInboxComponent, NoopAnimationsModule],
      providers: [
        { provide: NotificationService, useValue: notifService },
        { provide: Router, useValue: routerMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationInboxComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call list() on init', () => {
    expect(notifService.list).toHaveBeenCalledWith({ page: 1, pageSize: 20, unreadOnly: false });
  });

  it('should render notification items from signal', () => {
    notifsSig.set([makeNotif(), makeNotif({ id: 'n2', title: 'Second' })]);
    fixture.detectChanges();
    const items = fixture.nativeElement.querySelectorAll('.notification-item');
    expect(items.length).toBe(2);
  });

  it('should highlight unread notifications', () => {
    notifsSig.set([makeNotif({ isRead: false })]);
    fixture.detectChanges();
    const item = fixture.nativeElement.querySelector('.notification-item');
    expect(item.classList).toContain('unread');
  });

  it('onNotificationClick() should call markRead and navigate', async () => {
    const notif = makeNotif();
    component.onNotificationClick(notif);
    await fixture.whenStable();
    expect(notifService.markRead).toHaveBeenCalledWith('n1');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/tickets', '42']);
  });

  it('onMarkAllRead() should call markAllRead()', () => {
    component.onMarkAllRead();
    expect(notifService.markAllRead).toHaveBeenCalled();
  });

  it('toggleUnreadOnly() should reload list with unreadOnly=true', () => {
    component.toggleUnreadOnly();
    expect(component.unreadOnly()).toBe(true);
    expect(notifService.list).toHaveBeenCalledWith({ page: 1, pageSize: 20, unreadOnly: true });
  });

  it('loadMore() should request next page', () => {
    component['currentPage'].set(1);
    component.loadMore();
    expect(notifService.list).toHaveBeenCalledWith({
      page: 2,
      pageSize: 20,
      unreadOnly: component.unreadOnly(),
    });
  });

  it('should emit closePanel when close button clicked', () => {
    const emitSpy = vi.spyOn(component.closePanel, 'emit');
    component.close();
    expect(emitSpy).toHaveBeenCalled();
  });

  it('should truncate body longer than 80 characters', () => {
    const longBody = 'a'.repeat(100);
    expect(component.truncate(longBody)).toBe('a'.repeat(80) + '…');
  });

  it('should return correct navigation route for each entity type', () => {
    expect(component.entityRoute('ticket', '5')).toEqual(['/tickets', '5']);
    expect(component.entityRoute('article', '9')).toEqual(['/kb/articles', '9']);
    expect(component.entityRoute('chat', '3')).toEqual(['/chats', '3']);
  });
});
