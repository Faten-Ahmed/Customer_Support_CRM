import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CategoryService } from './category.service';

describe('CategoryService', () => {
  let service: CategoryService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), CategoryService],
    });
    service = TestBed.inject(CategoryService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list() GETs /api/v1/admin/categories', () => {
    service.list().subscribe();
    const req = http.expectOne('/api/v1/admin/categories');
    expect(req.request.method).toBe('GET');
    req.flush({ data: [] });
  });

  it('create() POSTs /api/v1/admin/categories', () => {
    service.create({ name: 'Hardware', sortOrder: 1 }).subscribe();
    const req = http.expectOne('/api/v1/admin/categories');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('Hardware');
    req.flush({ id: 'cat1' });
  });

  it('deactivate() POSTs /api/v1/admin/categories/{id}/deactivate', () => {
    service.deactivate('cat1').subscribe();
    const req = http.expectOne('/api/v1/admin/categories/cat1/deactivate');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'cat1', isActive: false });
  });

  it('reactivate() POSTs /api/v1/admin/categories/{id}/reactivate', () => {
    service.reactivate('cat1').subscribe();
    const req = http.expectOne('/api/v1/admin/categories/cat1/reactivate');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'cat1', isActive: true });
  });
});
