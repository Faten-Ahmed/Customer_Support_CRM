import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { NotificationService, Notification } from './notification.service';

const MOCK_NOTIFICATION: Notification = {
  id: 'n1',
  type: 'TicketAssigned',
  title: 'Ticket assigned to you',
  body: 'Ticket #42 has been assigned to your queue.',
  isRead: false,
  entityType: 'ticket',
  entityId: '42',
  createdAt: '2026-08-01T10:00:00Z',
};

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        NotificationService,
      ],
    });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('list() should GET /api/notifications with params', () => {
    service.list({ page: 1, pageSize: 20, unreadOnly: true }).subscribe();
    const req = httpMock.expectOne(r =>
      r.url === '/api/notifications' &&
      r.params.get('page') === '1' &&
      r.params.get('pageSize') === '20' &&
      r.params.get('unreadOnly') === 'true'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [MOCK_NOTIFICATION], totalCount: 1, page: 1, pageSize: 20 });
  });

  it('list() should populate notifications signal', () => {
    service.list({ page: 1, pageSize: 20 }).subscribe();
    const req = httpMock.expectOne(r => r.url === '/api/notifications');
    req.flush({ items: [MOCK_NOTIFICATION], totalCount: 1, page: 1, pageSize: 20 });
    expect(service.notifications()).toEqual([MOCK_NOTIFICATION]);
  });

  it('markRead() should PUT /api/notifications/{id}/read', () => {
    service.markRead('n1').subscribe();
    const req = httpMock.expectOne('/api/notifications/n1/read');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('markRead() should update notification isRead signal locally', () => {
    service['_notifications'].set([MOCK_NOTIFICATION]);
    service.markRead('n1').subscribe();
    const req = httpMock.expectOne('/api/notifications/n1/read');
    req.flush({});
    const updated = service.notifications().find(n => n.id === 'n1');
    expect(updated?.isRead).toBe(true);
  });

  it('markAllRead() should PUT /api/notifications/mark-all-read', () => {
    service.markAllRead().subscribe();
    const req = httpMock.expectOne('/api/notifications/mark-all-read');
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('markAllRead() should set all notifications isRead to true', () => {
    service['_notifications'].set([MOCK_NOTIFICATION, { ...MOCK_NOTIFICATION, id: 'n2' }]);
    service.markAllRead().subscribe();
    const req = httpMock.expectOne('/api/notifications/mark-all-read');
    req.flush({});
    expect(service.notifications().every(n => n.isRead)).toBe(true);
    expect(service.unreadCount()).toBe(0);
  });

  it('getUnreadCount() should GET /api/notifications/unread-count and update signal', () => {
    service.getUnreadCount().subscribe();
    const req = httpMock.expectOne('/api/notifications/unread-count');
    expect(req.request.method).toBe('GET');
    req.flush({ count: 7 });
    expect(service.unreadCount()).toBe(7);
  });

  it('pushNotification() should prepend to notifications signal and increment unreadCount', () => {
    service['_unreadCount'].set(2);
    service['_notifications'].set([]);
    service.pushNotification(MOCK_NOTIFICATION);
    expect(service.notifications()[0]).toEqual(MOCK_NOTIFICATION);
    expect(service.unreadCount()).toBe(3);
  });

  it('pushNotification() already-read should not increment unreadCount', () => {
    service['_unreadCount'].set(3);
    service.pushNotification({ ...MOCK_NOTIFICATION, isRead: true });
    expect(service.unreadCount()).toBe(3);
  });

  it('setUnreadCount() should update signal directly', () => {
    service.setUnreadCount(12);
    expect(service.unreadCount()).toBe(12);
  });
});
