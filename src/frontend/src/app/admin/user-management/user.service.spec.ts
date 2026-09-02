import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { UserService } from './user.service';

describe('UserService', () => {
  let service: UserService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), UserService],
    });
    service = TestBed.inject(UserService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list() GETs /api/v1/admin/users with role filter', () => {
    service.list({ page: 1, pageSize: 20, role: 'Agent' }).subscribe();
    const req = http.expectOne(r => r.url === '/api/v1/admin/users');
    expect(req.request.params.get('role')).toBe('Agent');
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  });

  it('create() POSTs /api/v1/admin/users', () => {
    service
      .create({
        firstName: 'Omar',
        lastName: 'Ali',
        email: 'omar@test.com',
        role: 'Agent',
        tempPassword: 'Temp1234!',
        primaryDepartmentId: 'd1',
      })
      .subscribe();
    const req = http.expectOne('/api/v1/admin/users');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'u1' });
  });

  it('deactivate() POSTs /api/v1/admin/users/{id}/deactivate', () => {
    service.deactivate('u1').subscribe();
    const req = http.expectOne('/api/v1/admin/users/u1/deactivate');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'u1', isActive: false });
  });

  it('updateDepartments() PUTs /api/v1/admin/users/{id}/departments', () => {
    service
      .updateDepartments('u1', [{ departmentId: 'd1', isPrimary: true }])
      .subscribe();
    const req = http.expectOne('/api/v1/admin/users/u1/departments');
    expect(req.request.method).toBe('PUT');
    req.flush([]);
  });
});
