import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TemplateService } from './template.service';

describe('TemplateService', () => {
  let service: TemplateService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        TemplateService,
      ],
    });
    service = TestBed.inject(TemplateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/templates', () => {
    service.list().subscribe();
    const req = httpMock.expectOne('/api/v1/templates');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0 });
  });

  it('render() should POST /api/v1/admin/templates/{id}/render with ticketId', () => {
    service.render('tpl-1', 't1').subscribe();
    const req = httpMock.expectOne('/api/v1/admin/templates/tpl-1/render');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ ticketId: 't1' });
    req.flush({ content: 'Dear customer, ...' });
  });
});
