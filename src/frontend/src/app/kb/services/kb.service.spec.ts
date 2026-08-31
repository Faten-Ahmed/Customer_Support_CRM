import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { KbService } from './kb.service';

describe('KbService', () => {
  let service: KbService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), KbService],
    });
    service = TestBed.inject(KbService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list() GETs /api/v1/kb/articles with status param', () => {
    service.list({ page: 1, pageSize: 20, status: 'Draft' }).subscribe();
    const req = http.expectOne(r => r.url === '/api/v1/kb/articles');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('status')).toBe('Draft');
    req.flush({ data: [], total: 0 });
  });

  it('search() GETs /api/v1/kb/search with q param', () => {
    service.search('reset password').subscribe();
    const req = http.expectOne(r => r.url === '/api/v1/kb/search');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('q')).toBe('reset password');
    req.flush([]);
  });

  it('create() POSTs /api/v1/kb/articles', () => {
    service.create({ title: 'How to reset', content: '# Step 1', categoryId: 'c1', visibility: 'Public' }).subscribe();
    const req = http.expectOne('/api/v1/kb/articles');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'art-1', status: 'Draft' });
  });

  it('update() PATCHes /api/v1/kb/articles/{id}', () => {
    service.update('art-1', { title: 'Updated' }).subscribe();
    const req = http.expectOne('/api/v1/kb/articles/art-1');
    expect(req.request.method).toBe('PATCH');
    req.flush({ id: 'art-1' });
  });

  it('submitForReview() POSTs /api/v1/kb/articles/{id}/submit-review', () => {
    service.submitForReview('art-1').subscribe();
    const req = http.expectOne('/api/v1/kb/articles/art-1/submit-review');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'art-1', status: 'PendingReview' });
  });

  it('approve() POSTs /api/v1/kb/articles/{id}/approve', () => {
    service.approve('art-1').subscribe();
    const req = http.expectOne('/api/v1/kb/articles/art-1/approve');
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'art-1', status: 'Published' });
  });

  it('reject() POSTs /api/v1/kb/articles/{id}/reject with rejectionNote', () => {
    service.reject('art-1', 'Needs more detail here').subscribe();
    const req = http.expectOne('/api/v1/kb/articles/art-1/reject');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ rejectionNote: 'Needs more detail here' });
    req.flush({ id: 'art-1', status: 'Draft' });
  });

  it('archive() POSTs /api/v1/kb/articles/{id}/archive', () => {
    service.archive('art-1').subscribe();
    const req = http.expectOne('/api/v1/kb/articles/art-1/archive');
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('delete() DELETEs /api/v1/kb/articles/{id}', () => {
    service.delete('art-1').subscribe();
    const req = http.expectOne('/api/v1/kb/articles/art-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
