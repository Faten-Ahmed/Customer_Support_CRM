import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { DepartmentService } from './department.service';

describe('DepartmentService', () => {
  let service: DepartmentService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), DepartmentService],
    });
    service = TestBed.inject(DepartmentService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list() GETs /api/v1/admin/departments', () => {
    service.list().subscribe();
    const req = http.expectOne('/api/v1/admin/departments');
    expect(req.request.method).toBe('GET');
    req.flush({ data: [] });
  });

  it('create() POSTs /api/v1/admin/departments', () => {
    service.create({ name: 'Support', nameAr: 'الدعم' }).subscribe();
    const req = http.expectOne('/api/v1/admin/departments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('Support');
    req.flush({ id: 'd1' });
  });

  it('deactivate() POSTs /api/v1/admin/departments/{id}/deactivate', () => {
    service.deactivate('d1').subscribe();
    const req = http.expectOne('/api/v1/admin/departments/d1/deactivate');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'd1', isActive: false });
  });

  it('reactivate() POSTs /api/v1/admin/departments/{id}/reactivate', () => {
    service.reactivate('d1').subscribe();
    const req = http.expectOne('/api/v1/admin/departments/d1/reactivate');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'd1', isActive: true });
  });
});
