import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { BusinessHoursService, BusinessHoursCard } from './business-hours.service';

const mockCard: BusinessHoursCard = {
  id: 'bh-global',
  departmentId: null,
  workDays: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday'],
  startTime: '08:00',
  endTime: '17:00',
  timeZone: 'Asia/Riyadh',
  holidays: [{ id: 'hol-1', date: '2026-01-01', name: 'New Year' }],
};

describe('BusinessHoursService', () => {
  let service: BusinessHoursService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), BusinessHoursService],
    });
    service = TestBed.inject(BusinessHoursService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('list() GETs /api/v1/admin/business-hours', () => {
    service.list().subscribe(cards => {
      expect(cards.length).toBe(1);
      expect(cards[0].timeZone).toBe('Asia/Riyadh');
    });
    const req = http.expectOne('/api/v1/admin/business-hours');
    expect(req.request.method).toBe('GET');
    req.flush([mockCard]);
  });

  it('update() PUTs /api/v1/admin/business-hours/{id}', () => {
    const payload = { workDays: ['Monday'], startTime: '09:00', endTime: '18:00', timeZone: 'UTC' };
    service.update('bh-global', payload).subscribe();
    const req = http.expectOne('/api/v1/admin/business-hours/bh-global');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.workDays).toEqual(['Monday']);
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('addHoliday() POSTs /api/v1/admin/business-hours/{id}/holidays', () => {
    service.addHoliday('bh-global', '2026-12-25', 'Christmas').subscribe(res => {
      expect(res.id).toBe('hol-2');
    });
    const req = http.expectOne('/api/v1/admin/business-hours/bh-global/holidays');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ date: '2026-12-25', name: 'Christmas' });
    req.flush({ id: 'hol-2' });
  });

  it('deleteHoliday() DELETEs /api/v1/admin/business-hours/{id}/holidays/{holidayId}', () => {
    service.deleteHoliday('bh-global', 'hol-1').subscribe();
    const req = http.expectOne('/api/v1/admin/business-hours/bh-global/holidays/hol-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
