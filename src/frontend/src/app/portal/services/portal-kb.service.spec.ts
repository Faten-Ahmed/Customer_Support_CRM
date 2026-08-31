import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PortalKbService } from './portal-kb.service';

describe('PortalKbService', () => {
  let service: PortalKbService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), PortalKbService],
    });
    service = TestBed.inject(PortalKbService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list() GETs /api/v1/portal/kb/articles', () => {
    service.list().subscribe();
    const req = http.expectOne(r => r.url === '/api/v1/portal/kb/articles');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0 });
  });

  it('list() passes categoryId param', () => {
    service.list({ categoryId: 'c1' }).subscribe();
    const req = http.expectOne(r => r.url === '/api/v1/portal/kb/articles');
    expect(req.request.params.get('categoryId')).toBe('c1');
    req.flush({ items: [], totalCount: 0 });
  });

  it('search() GETs /api/v1/portal/kb/search with q param', () => {
    service.search('password reset').subscribe();
    const req = http.expectOne(r => r.url === '/api/v1/portal/kb/search');
    expect(req.request.params.get('q')).toBe('password reset');
    req.flush([]);
  });

  it('getById() GETs /api/v1/portal/kb/articles/:id', () => {
    service.getById('a1').subscribe();
    const req = http.expectOne('/api/v1/portal/kb/articles/a1');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'a1', title: 'Test', content: 'Content here.', status: 'Published',
      visibility: 'Public', categoryId: 'c1', createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString() });
  });

  it('getCategories() GETs /api/v1/portal/kb/categories', () => {
    service.getCategories().subscribe();
    const req = http.expectOne('/api/v1/portal/kb/categories');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
