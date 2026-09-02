import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { FieldDefinitionService } from './field-definition.service';

describe('FieldDefinitionService', () => {
  let service: FieldDefinitionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        FieldDefinitionService,
      ],
    });
    service = TestBed.inject(FieldDefinitionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() should GET /api/v1/admin/field-definitions with departmentId', () => {
    service.list('d1').subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/admin/field-definitions');
    expect(req.request.params.get('departmentId')).toBe('d1');
    req.flush([{ id: 'f1', label: 'Account #', type: 'text', required: true }]);
  });
});
